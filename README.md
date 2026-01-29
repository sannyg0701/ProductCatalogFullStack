# Product Catalog - Full Stack Application

A full-stack product catalog application built with C# .NET 8 Web API and Angular 18, demonstrating clean architecture principles, RESTful API design, and modern development practices.

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20.x](https://nodejs.org/) (LTS version)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for SQL Server)
- [Angular CLI 18](https://angular.io/cli) (`npm install -g @angular/cli@18`)

### Option 1: Run with Docker Compose (Recommended)

```bash
# Start all services (SQL Server, API, Frontend)
docker-compose up -d

# Access the application
# Frontend: http://localhost:4200
# API Swagger: http://localhost:5000/swagger/index.html
```

### Option 2: Run Manually

**1. Start SQL Server:**
```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong!Passw0rd" \
  -p 1433:1433 --name productcatalog-sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

**2. Run the API:**
```bash
cd ProductCatalog/src/ProductCatalog.Api
dotnet run --urls "http://localhost:5000"
```

**3. Run the Frontend:**
```bash
cd ProductCatalogUI
npm install
ng serve
```

**4. Access the application:**
- Frontend: http://localhost:4200
- API Swagger: http://localhost:5000/swagger/index.html

---

## Architecture

### Overall Architecture Approach

The solution follows **Clean Architecture** principles with clear separation of concerns across three layers:

```
ProductCatalog/
├── src/
│   ├── ProductCatalog.Api/           # Presentation Layer
│   │   ├── Controllers/              # Thin controllers, HTTP concerns only
│   │   └── Middleware/               # Cross-cutting concerns (exception handling)
│   │
│   ├── ProductCatalog.Core/          # Domain Layer (innermost)
│   │   ├── Entities/                 # Domain models
│   │   ├── DTOs/                     # Data transfer objects
│   │   └── Interfaces/               # Abstractions (repositories, services)
│   │
│   └── ProductCatalog.Infrastructure/  # Infrastructure Layer
│       ├── Data/                     # EF Core DbContext, configurations, migrations
│       ├── Repositories/             # Data access implementations
│       └── Services/                 # Business logic implementations
│
└── tests/
    └── ProductCatalog.Tests/         # Unit tests

ProductCatalogUI/                     # Angular 18 Frontend
├── src/app/
│   ├── components/                   # UI components
│   ├── services/                     # HTTP services
│   └── models/                       # TypeScript interfaces
```

**Dependency Flow:** Api -> Core <- Infrastructure

The Core layer has no dependencies on external frameworks, making it highly testable and maintainable.

### Database Schema

```
┌─────────────────────────────────────┐       ┌─────────────────────────────┐
│            Products                 │       │         Categories          │
├─────────────────────────────────────┤       ├─────────────────────────────┤
│ Id           INT (PK, Identity)     │       │ Id          INT (PK)        │
│ Name         NVARCHAR(200) NOT NULL │       │ Name        NVARCHAR(100)   │
│ Description  NVARCHAR(2000) NULL    │       │ Description NVARCHAR(500)   │
│ Price        DECIMAL(18,2) NOT NULL │       │ IsActive    BIT             │
│ CategoryId   INT (FK) NOT NULL      │───────│                             │
│ StockQuantity INT NOT NULL          │       └─────────────────────────────┘
│ CreatedDate  DATETIME2 NOT NULL     │
│ IsActive     BIT NOT NULL           │
└─────────────────────────────────────┘

Indexes:
- IX_Products_CategoryId        (CategoryId)           - FK lookups, category filtering
- IX_Products_IsActive          (IsActive)             - Active product filtering
- IX_Products_Price             (Price)                - Price range queries
- IX_Products_Name              (Name)                 - Sorting, search optimization
- IX_Products_CreatedDate       (CreatedDate)          - Date sorting
```

### Technology Choices

| Layer | Technology | Rationale |
|-------|------------|-----------|
| Backend | .NET 8 | LTS version, excellent performance, native AOT support |
| ORM | Entity Framework Core 8 | Mature, LINQ support, migrations, well-documented |
| Database | SQL Server 2022 | Enterprise-grade, excellent EF Core support |
| Frontend | Angular 18 | Standalone components, signals, improved performance |
| Containerization | Docker | Consistent environments, easy deployment |

---

## Design Decisions

### Single Responsibility & Dependency Inversion

**Single Responsibility Principle (SRP):**
- **Controllers** handle only HTTP concerns (routing, model binding, response formatting)
- **Services** contain business logic and orchestration
- **Repositories** handle data access and query construction
- **Middleware** handles cross-cutting concerns (exception handling)

Example - ProductsController is thin:
```csharp
[HttpGet("{id:int}")]
public async Task<ActionResult<ProductResponse>> GetById(int id, CancellationToken cancellationToken)
{
    var product = await _productService.GetByIdAsync(id, cancellationToken);
    if (product is null) return NotFound();
    return Ok(product);
}
```

**Dependency Inversion Principle (DIP):**
- Core layer defines interfaces (`IProductService`, `IProductRepository`)
- Infrastructure layer implements these interfaces
- Api layer depends only on abstractions via constructor injection

```csharp
// Registration in Program.cs
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
```

### Entity Framework Core Approach & Query Optimization

**Key optimizations implemented:**

1. **AsNoTracking()** for all read operations - reduces memory overhead:
```csharp
return await _context.Products
    .Where(p => p.IsActive)
    .AsNoTracking()
    .ToListAsync(cancellationToken);
```

2. **Projection to DTOs** - avoids over-fetching and N+1 queries:
```csharp
.Select(p => new ProductResponse
{
    Id = p.Id,
    Name = p.Name,
    CategoryName = p.Category.Name,  // Single query with JOIN
    // ...
})
```

3. **Efficient pagination** with Skip/Take after filtering:
```csharp
var totalCount = await query.CountAsync(cancellationToken);  // Count before pagination
var items = await query
    .Skip((request.PageNumber - 1) * request.PageSize)
    .Take(request.PageSize)
    .ToListAsync(cancellationToken);
```

4. **ConfigureAwait(false)** throughout service/repository layer for better performance in library code.

### Complex Endpoint Choice: Product Search (Option A)

**Rationale:** Product search is the more practical choice for a catalog application - it directly supports the frontend's filtering, sorting, and pagination needs.

**Implementation highlights:**

- **All parameters optional and combinable** - builds query dynamically
- **Case-insensitive search** with AND logic for multiple terms:
```csharp
var searchTerms = request.SearchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
foreach (var term in searchTerms)
{
    query = query.Where(p =>
        p.Name.ToLower().Contains(term.ToLower()) ||
        p.Description.ToLower().Contains(term.ToLower()));
}
```
- **Single efficient query** - filtering, sorting, and pagination in one database round-trip
- **Strongly-typed response** with pagination metadata:
```json
{
  "items": [...],
  "totalCount": 50,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 5,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

### Repository Pattern Decision & Trade-offs

**Decision:** Implemented the Repository pattern.

**Benefits:**
- **Testability** - Services can be unit tested with mock repositories
- **Separation of concerns** - Query logic isolated from business logic
- **Consistency** - Centralized data access patterns
- **Flexibility** - Can swap data access strategy without changing services

**Trade-offs:**
- **Additional abstraction layer** - More files and interfaces to maintain
- **Potential for leaky abstractions** - IQueryable exposure can leak EF concerns
- **Overhead for simple CRUD** - Direct DbContext might be simpler for basic operations

**Mitigation:** Repositories return DTOs directly for read operations, keeping EF concerns contained:
```csharp
// Repository returns DTO, not Entity
Task<ProductResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
```

**Alternative considered:** Using DbContext directly in services is valid for smaller applications. The repository pattern was chosen here to demonstrate clean architecture principles and support potential future complexity (caching, multiple data sources).

### Index Strategy

| Index | Columns | Query Patterns Supported |
|-------|---------|-------------------------|
| `IX_Products_CategoryId` | CategoryId | Category filtering, FK joins |
| `IX_Products_IsActive` | IsActive | All queries filter by active status |
| `IX_Products_Price` | Price | Price range filtering (minPrice, maxPrice) |
| `IX_Products_Name` | Name | Default sort order, search optimization |
| `IX_Products_CreatedDate` | CreatedDate | Date-based sorting |

**Rationale:**
- Every search query filters by `IsActive` - index eliminates full table scans
- `CategoryId` index supports both FK constraint and category filtering
- Price and date indexes support the search endpoint's sort options
- Composite indexes were considered but single-column indexes provide flexibility for various query combinations

---

## What I Would Do With More Time

### Unimplemented Features

1. **Authentication & Authorization**
   - JWT-based authentication
   - Role-based access control (Admin for CRUD, Public for read-only)
   - API key support for external integrations

2. **Caching**
   - Redis distributed cache for product listings
   - Cache invalidation on product updates
   - ETag support for conditional requests

3. **Advanced Search**
   - Full-text search with SQL Server FTS or Elasticsearch
   - Search suggestions/autocomplete
   - Faceted search with category counts

4. **API Enhancements**
   - API versioning (URL or header-based)
   - Rate limiting
   - HATEOAS links in responses
   - Bulk operations endpoint

### Refactoring Priorities

1. **Specification Pattern** - Extract query logic into reusable specifications
2. **Result Pattern** - Replace exceptions with Result<T> for expected failures
3. **MediatR/CQRS** - Separate read and write models for complex scenarios
4. **Integration Tests** - Add tests with TestContainers for database scenarios

### Production Considerations

1. **Observability**
   - Structured logging with Serilog
   - Application Insights or OpenTelemetry
   - Health check dashboard

2. **Security**
   - HTTPS enforcement
   - CORS configuration for production domains
   - Input sanitization review
   - SQL injection protection (already handled by EF Core parameterization)

3. **Performance**
   - Response compression
   - Connection pooling configuration
   - Database query plan analysis
   - Load testing with k6 or NBomber

4. **Deployment**
   - CI/CD pipeline (GitHub Actions)
   - Kubernetes manifests (included in `/k8s`)
   - Environment-specific configuration
   - Database migration strategy

---

## Assumptions & Trade-offs

### Key Assumptions

1. **Single-tenant application** - No multi-tenancy requirements
2. **Moderate scale** - Hundreds of products, not millions (affects indexing strategy)
3. **SQL Server available** - Docker-based for development, managed instance for production
4. **Soft deletes preferred** - Data retention requirements; hard deletes not needed
5. **UTC timestamps** - All dates stored in UTC, frontend handles timezone display

### Trade-offs in Design

| Decision | Trade-off | Rationale |
|----------|-----------|-----------|
| Repository pattern | More abstraction vs. simplicity | Chose testability and separation of concerns |
| Sync DB migrations on startup | Startup delay vs. manual migration | Simpler development experience; would change for production |
| DTOs in Core layer | Slight coupling vs. separate contracts project | Pragmatic choice for project size |
| No caching | Simplicity vs. performance | Out of scope; would add Redis for production |
| SQL Server | Heavier than SQLite vs. production parity | Matches likely production environment |

### API Design Decisions

- **Soft delete returns 204** - Consistent with DELETE semantics; client doesn't need the updated entity
- **Search returns empty array, not 404** - Empty results are valid; 404 is for missing resources
- **Validation at DTO level** - Fail fast before hitting service layer
- **Category validation on product create/update** - Ensures referential integrity at application level

---

## API Endpoints

### Products
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products` | Get all active products |
| GET | `/api/products/{id}` | Get product by ID |
| GET | `/api/products/search` | Search with filters, sorting, pagination |
| POST | `/api/products` | Create new product |
| PUT | `/api/products/{id}` | Update product |
| DELETE | `/api/products/{id}` | Soft delete product |

### Categories
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/categories` | Get all active categories |
| POST | `/api/categories` | Create new category |

### Health Checks
| Endpoint | Description |
|----------|-------------|
| `/health/live` | Liveness probe - app is running |
| `/health/ready` | Readiness probe - dependencies healthy |

---

## Project Structure

```
ProductCatalogFullStack/
├── ProductCatalog/                 # Backend solution
│   ├── src/
│   │   ├── ProductCatalog.Api/
│   │   ├── ProductCatalog.Core/
│   │   └── ProductCatalog.Infrastructure/
│   ├── tests/
│   │   └── ProductCatalog.Tests/
│   └── k8s/                        # Kubernetes manifests
├── ProductCatalogUI/               # Angular frontend
├── docker-compose.yml              # Full stack orchestration
└── README.md
```

---

## Testing

### Run Backend Tests
```bash
cd ProductCatalog/tests/ProductCatalog.Tests
dotnet test
```

### Test Coverage
- Unit tests for services (business logic)
- Unit tests for controllers (HTTP behavior)
- Mocking with Moq for dependencies

---

## License

This project was created as a technical assessment demonstration.
