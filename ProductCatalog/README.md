# Product Catalog API

A high-performance RESTful API for managing a product catalog, built with .NET 8 and clean architecture principles. Designed to scale to thousands of requests per minute.

## Table of Contents

- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [API Endpoints](#api-endpoints)
- [Running with Docker](#running-with-docker)
- [Kubernetes Deployment](#kubernetes-deployment)
- [Testing](#testing)
- [Production Considerations](#production-considerations)
- [Angular Frontend](#angular-frontend)

## Architecture

### Solution Structure

```
assessment/
├── ProductCatalog/                    # Backend API
│   ├── src/
│   │   ├── ProductCatalog.Api/           # ASP.NET Core Web API
│   │   ├── ProductCatalog.Core/          # Domain layer (entities, interfaces, DTOs)
│   │   └── ProductCatalog.Infrastructure/ # Data access (EF Core, repositories, services)
│   ├── tests/
│   │   └── ProductCatalog.Tests/         # Unit tests (NUnit + Moq)
│   ├── k8s/                              # Kubernetes manifests
│   └── docker-compose.yml
│
└── product-catalog-ui/                # Angular Frontend
    └── src/app/
        ├── models/                       # TypeScript interfaces
        ├── services/                     # HTTP services (DI)
        └── components/                   # UI components
```

### Design Patterns

- **Clean Architecture**: Separation of concerns with Core (domain), Infrastructure (data), and Api (presentation) layers
- **Repository Pattern**: Abstracts data access, enables testability
- **Dependency Injection**: All dependencies injected via constructors
- **Soft Delete**: Products are deactivated rather than permanently deleted

### Database Schema

- **Products**: Id, Name, Description, Price, CategoryId, StockQuantity, CreatedDate, IsActive
- **Categories**: Id, Name, Description, IsActive

Indexes optimized for common query patterns (category filtering, price range, active status).

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20.x](https://nodejs.org/) (for Angular frontend)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for containerized deployment)
- SQL Server (via Docker or local installation)

### Local Development

1. **Start SQL Server** (using Docker):
   ```bash
   docker-compose up -d sqlserver
   ```

2. **Run the API**:
   ```bash
   cd src/ProductCatalog.Api
   dotnet run
   ```

3. **Access Swagger UI**: Navigate to `http://localhost:5000/swagger`

The database is automatically created and seeded with sample data on first run.

### Running the Angular Frontend

1. **Install dependencies** (first time only):
   ```bash
   cd product-catalog-ui
   npm install
   ```

2. **Start the development server**:
   ```bash
   ng serve
   ```

3. **Open the app**: Navigate to `http://localhost:4200`

The frontend connects to the API at `http://localhost:5000`. Ensure the API is running before starting the frontend.

### Configuration

Connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=ProductCatalog;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True"
  }
}
```

## API Endpoints

### Products

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products` | Get all active products |
| GET | `/api/products/{id}` | Get product by ID |
| GET | `/api/products/search` | Search with filters, sorting, pagination |
| POST | `/api/products` | Create a new product |
| PUT | `/api/products/{id}` | Update an existing product |
| DELETE | `/api/products/{id}` | Soft delete a product |

### Categories

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/categories` | Get all active categories |
| POST | `/api/categories` | Create a new category |

### Search Parameters

The `/api/products/search` endpoint supports:

| Parameter | Type | Description |
|-----------|------|-------------|
| searchTerm | string | Search in name and description (AND logic for multiple terms) |
| categoryId | int | Filter by category |
| minPrice | decimal | Minimum price filter |
| maxPrice | decimal | Maximum price filter |
| inStock | bool | Filter by stock availability |
| sortBy | string | Sort field: name, price, createddate, stockquantity |
| sortOrder | string | asc or desc |
| pageNumber | int | Page number (default: 1) |
| pageSize | int | Items per page (default: 10, max: 100) |

### Health Checks

| Endpoint | Description |
|----------|-------------|
| `/health/live` | Liveness probe - checks if app is running |
| `/health/ready` | Readiness probe - checks database connectivity |

## Running with Docker

### Build and Run

```bash
# Build and start all services
docker-compose up --build

# Or run in detached mode
docker-compose up -d --build
```

The API will be available at `http://localhost:5000`.

### Docker Compose Services

- **sqlserver**: SQL Server 2022 database
- **api**: Product Catalog API

## Kubernetes Deployment

### Prerequisites

- Kubernetes cluster (Docker Desktop with K8s enabled, minikube, or cloud provider)
- kubectl configured

### Deploy

```bash
# Build the Docker image
docker build -t productcatalog-api:latest -f src/ProductCatalog.Api/Dockerfile .

# Apply Kubernetes manifests
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secret.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
kubectl apply -f k8s/hpa.yaml
```

### Kubernetes Resources

- **Namespace**: `productcatalog` - Isolated environment
- **ConfigMap**: Environment configuration
- **Secret**: Database connection string
- **Deployment**: 2 replicas with resource limits, health probes
- **Service**: ClusterIP for internal access
- **HPA**: Auto-scaling (2-10 pods based on CPU/memory)

### Verify Deployment

```bash
kubectl get all -n productcatalog
kubectl logs -f deployment/productcatalog-api -n productcatalog
```

## Testing

### Run Unit Tests

```bash
dotnet test
```

### Test Coverage

- **Service Tests**: Business logic validation, category existence checks, soft delete behavior
- **Controller Tests**: HTTP status codes, request/response handling, error scenarios

### Manual Testing

Use the Swagger UI at `/swagger` or tools like curl/Postman:

```bash
# Get all products
curl http://localhost:5000/api/products

# Search products
curl "http://localhost:5000/api/products/search?searchTerm=laptop&minPrice=500&maxPrice=2000"

# Create a product
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -d '{"name":"New Product","price":29.99,"categoryId":1,"stockQuantity":100}'
```

## Production Considerations

### Implemented

- **EF Core Retry Policy**: Automatic retry on transient SQL failures (3 retries, 5s max delay)
- **Global Exception Handling**: Consistent error responses with ProblemDetails
- **Health Checks**: Liveness and readiness probes for orchestrator integration
- **Database Indexes**: Optimized for filtering, sorting, and pagination queries
- **Soft Delete**: Data preservation for audit and recovery

### Recommended for Production

- **Resiliency with Polly**: Add circuit breaker and retry policies for external service calls
  ```csharp
  services.AddHttpClient<IExternalService>()
      .AddPolicyHandler(GetRetryPolicy())
      .AddPolicyHandler(GetCircuitBreakerPolicy());
  ```

- **Distributed Caching**: Redis for frequently accessed data
  ```csharp
  services.AddStackExchangeRedisCache(options =>
  {
      options.Configuration = "redis-server:6379";
  });
  ```

- **Response Caching**: ETags and Cache-Control headers for GET endpoints

- **Observability**: OpenTelemetry for distributed tracing
  ```csharp
  services.AddOpenTelemetry()
      .WithTracing(builder => builder
          .AddAspNetCoreInstrumentation()
          .AddSqlClientInstrumentation());
  ```

- **API Versioning**: Support multiple API versions for backward compatibility

- **Rate Limiting**: Protect against abuse with ASP.NET Core rate limiting middleware

- **Secrets Management**: Use Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault

### Scaling Strategies

- **Horizontal Pod Autoscaler**: Configured to scale 2-10 pods based on CPU (70%) and memory (80%)
- **Database**: Consider read replicas for read-heavy workloads
- **Caching Layer**: Redis cluster for session and data caching
- **CDN**: For static assets in frontend applications

## Angular Frontend

The Angular frontend (`product-catalog-ui/`) provides a complete product management interface.

### Features

- **Product List** - Display all products with search, filter, sort, and pagination
- **Product Details** - Dedicated view showing full product information
- **Create Product** - Form with validation to add new products
- **Edit Product** - Update existing products
- **Delete Product** - Soft delete with confirmation
- **Search & Filter** - Search by name/description, filter by category, price range, stock status
- **Sorting** - Click column headers to sort (Name, Price, Stock)
- **Pagination** - Navigate through large result sets

### Technical Implementation

| Concept | Implementation |
|---------|----------------|
| **Dependency Injection** | `inject()` function for services (Angular 18 pattern) |
| **HTTP Service** | ProductService and CategoryService with typed Observables |
| **Standalone Components** | Angular 18 pattern, no NgModule required |
| **Reactive Forms** | FormBuilder, FormGroup, Validators for product form |
| **Template-driven Binding** | `[(ngModel)]` for search filters |
| **Structural Directives** | `*ngFor` for looping, `*ngIf` for conditionals |
| **Route Parameters** | `:id` parameter for details and edit routes |
| **Environment Config** | API URL configured in environment files |

### Routes

| Route | Component | Description |
|-------|-----------|-------------|
| `/` | ProductListComponent | Product list with search/filter |
| `/products` | ProductListComponent | Same as above |
| `/products/new` | ProductFormComponent | Create new product |
| `/products/:id` | ProductDetailsComponent | View product details |
| `/products/:id/edit` | ProductFormComponent | Edit existing product |

### Project Structure

```
product-catalog-ui/src/
├── environments/
│   ├── environment.ts            # Development config (apiUrl)
│   └── environment.prod.ts       # Production config
└── app/
    ├── models/
    │   ├── product.model.ts      # Product, CreateProductRequest, PagedResult
    │   └── category.model.ts     # Category interface
    ├── services/
    │   ├── product.service.ts    # CRUD + search operations
    │   └── category.service.ts   # Category operations
    ├── components/
    │   ├── product-list/         # List with search, filter, sort, pagination
    │   ├── product-details/      # Read-only product view
    │   └── product-form/         # Create/Edit form with validation
    ├── app.config.ts             # Providers (HttpClient, Router)
    ├── app.routes.ts             # Route definitions
    └── app.component.ts          # Root component with router-outlet
```

### Angular Best Practices Applied

- **Environment configuration** - API URL in environment files, not hardcoded
- **Typed HTTP responses** - Generic types for API responses
- **Reactive Forms** - For complex forms with validation
- **Smart/Dumb components** - Services handle data, components handle display
- **Route parameters** - For resource identification (`/products/:id`)
- **Error handling** - User-friendly error messages on API failures

## Future Improvements

The following items are recommended but not yet implemented:

| Item | Priority | Notes |
|------|----------|-------|
| Integration Tests | Medium | Test API endpoints with real database (WebApplicationFactory) |
| Angular Unit Tests | Medium | Component and service tests with Jasmine/Karma |
| Docker Build Verification | Low | Verify containerized deployment works end-to-end |
| K8s Deployment Test | Low | Test manifests on local Docker Desktop Kubernetes |
| API Versioning | Low | Add `/api/v1/` prefix for future compatibility |

## License

This project is part of a technical assessment.
