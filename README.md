# Product Catalog - Full Stack Application

A full-stack product catalog app with a .NET 8 Web API and Angular 18. Focused on clean architecture, clear trade-offs, and a working search endpoint.

## Quick Start

### Prerequisites
- .NET 8 SDK
- Node.js 20.x
- Docker Desktop
- Angular CLI 18 (`npm install -g @angular/cli@18`)

### Option 1: Run with Docker Compose (Recommended)
```bash
cp .env.example .env
docker-compose up -d
```
- Frontend: http://localhost:4200
- API Swagger: http://localhost:5000/swagger/index.html

### Option 2: Run Manually
**1) Start SQL Server**
```bash
# Replace <YourPassword> with a strong password (8+ chars, upper/lower/digit/symbol)
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<YourPassword>" \
  -p 1433:1433 --name productcatalog-sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-CU15-ubuntu-22.04
```

**2) Configure the API**
```bash
cd ProductCatalog/src/ProductCatalog.Api
cp appsettings.Development.json.example appsettings.Development.json
```

**3) Run the API**
```bash
dotnet run --urls "http://localhost:5000"
```

**4) Run the Frontend**
```bash
cd ProductCatalogUI
npm install
ng serve
```

## Architecture
- Clean Architecture: Api (presentation), Core (domain), Infrastructure (data/services)
- Dependency flow: Api -> Core <- Infrastructure
- Thin controllers, service layer for business logic, repositories for data access

### Database Schema
- Products: Id, Name, Description, Price, CategoryId, StockQuantity, CreatedDate, IsActive
- Categories: Id, Name, Description, IsActive

### Technology Choices
- Backend: .NET 8
- ORM: EF Core 8
- Database: SQL Server 2022
- Frontend: Angular 18
- Containerization: Docker

## Design Decisions
### SRP & DIP
- Controllers only handle HTTP concerns.
- Services orchestrate business logic.
- Repositories isolate data access.
- Interfaces live in Core; implementations in Infrastructure.

### EF Core & Query Optimization
- AsNoTracking for read operations.
- Projection to DTOs to avoid over-fetching and N+1 queries.
- Search uses raw SQL for a true single-query implementation.

### Complex Endpoint Choice: Product Search (Option A)
- Search is the most practical for the UI.
- Raw SQL with CTE + UNION ALL returns TotalCount even when a page is empty.
- LIKE escaping for %, _, [, \\ to treat them literally.
- Stable pagination with secondary sort by Id.

### Repository Pattern
- Chosen for testability and separation of concerns.
- Trade-off: extra abstraction layer.

### Index Strategy
- IX_Products_CategoryId: category filtering / FK joins
- IX_Products_IsActive: active-only filtering
- IX_Products_Price: price range filtering
- IX_Products_Name: default sort and search
- IX_Products_CreatedDate: date sorting

## API Endpoints
**Products**
- GET /api/products
- GET /api/products/{id}
- GET /api/products/search
- POST /api/products
- PUT /api/products/{id}
- DELETE /api/products/{id}

**Categories**
- GET /api/categories
- POST /api/categories

**Health**
- GET /health/live
- GET /health/ready

## Testing
```bash
# All tests
cd ProductCatalog
dotnet test

# Unit tests only
dotnet test tests/ProductCatalog.Tests

# Integration tests only (requires Docker)
dotnet test tests/ProductCatalog.Tests.Integration
```

## What I Would Do With More Time
- Add auth/authorization (JWT + roles).
- Improve database resilience (retry policies + read replicas).

## Assumptions & Trade-offs
- Single-tenant app with moderate data size (not millions of rows).
- Soft delete preferred for auditability.
- Raw SQL used only for search to satisfy the single-query requirement.
- Seeding runs only in Development to avoid production startup issues.

## Frontend
Angular 18 app provides a product list view with basic routing and error handling. See `ProductCatalogUI/` for details.
