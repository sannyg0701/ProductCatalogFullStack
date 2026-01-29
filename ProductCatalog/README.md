# Product Catalog API

Minimal backend README focused on setup, architecture, and key decisions.

## Quick Start (API Only)

### Prerequisites
- .NET 8 SDK
- Docker Desktop (for SQL Server)

### Run with Docker Compose
```bash
cp ..\\.env.example ..\\.env
docker-compose up -d sqlserver api
```

### Run Manually
**1) Start SQL Server**
```bash
# Replace <YourPassword> with a strong password (8+ chars, upper/lower/digit/symbol)
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<YourPassword>" \
  -p 1433:1433 --name productcatalog-sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-CU15-ubuntu-22.04
```

**2) Configure and run the API**
```bash
cd src/ProductCatalog.Api
cp appsettings.Development.json.example appsettings.Development.json
dotnet run --urls "http://localhost:5000"
```

## Architecture (Backend)
- Clean Architecture: Api (presentation), Core (domain), Infrastructure (data/services)
- Dependency flow: Api -> Core <- Infrastructure
- Thin controllers, services for business logic, repositories for data access

## Design Decisions (Backend)
- EF Core for ORM; AsNoTracking for reads; DTO projections
- Product search uses raw SQL with CTE + UNION ALL to satisfy single-query + correct TotalCount
- LIKE escaping for %, _, [, \\ and stable pagination with secondary Id sort
- Seed data runs only in Development to avoid production startup issues

## API Endpoints
- GET /api/products
- GET /api/products/{id}
- GET /api/products/search
- POST /api/products
- PUT /api/products/{id}
- DELETE /api/products/{id}
- GET /api/categories
- POST /api/categories
- GET /health/live
- GET /health/ready

## Testing
```bash
dotnet test
dotnet test tests/ProductCatalog.Tests
dotnet test tests/ProductCatalog.Tests.Integration  # requires Docker
```
