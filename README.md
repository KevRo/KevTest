# KevTest

A .NET 8 solution demonstrating a layered service architecture for database access and
external API calls.

## Structure

```
KevTest.sln
src/
  KevTest.Core/      Entities, DTOs, and interfaces (IRepository, IExternalApiClient, IProductService)
  KevTest.Data/       EF Core DbContext (SQLite), generic Repository<T>, migrations
  KevTest.Services/   Business services (ProductService) + generic HTTP client wrapper (ExternalApiClient)
  KevTest.Api/        ASP.NET Core Web API host wiring everything together via DI
tests/
  KevTest.Tests/      xUnit tests for the service layer (EF Core InMemory + fake HttpMessageHandler)
```

## Architecture

- **KevTest.Core** defines the contracts (`IRepository<T>`, `IExternalApiClient`, `IProductService`)
  and shared entities/DTOs. It has no dependency on EF Core or ASP.NET Core.
- **KevTest.Data** implements `IRepository<T>` with EF Core against SQLite (`AppDbContext` in
  `src/KevTest.Data/AppDbContext.cs`). Swap the provider in
  `ServiceCollectionExtensions.AddDataLayer` (e.g. `UseSqlServer`/`UseNpgsql`) without touching
  any calling code.
- **KevTest.Services** contains the business/service layer. `ProductService` composes
  `IRepository<Product>` for data access; `ExternalApiClient` is a generic JSON HTTP wrapper
  around `HttpClient` for calling external APIs (`GetAsync`/`PostAsync`/`PutAsync`/`DeleteAsync`).
- **KevTest.Api** is a thin ASP.NET Core Web API that wires up DI (`AddDataLayer`,
  `AddServiceLayer`) and exposes `ProductsController` as an example consumer of the service layer.

## Running

```bash
dotnet build
dotnet run --project src/KevTest.Api
```

The API applies EF Core migrations automatically on startup, creating `kevtest.db` (SQLite) in
the working directory. Swagger UI is available at `/swagger` in Development.

Configure the external API base URL via `ExternalApi:BaseUrl` in `appsettings.json` or environment
variables.

## Testing

```bash
dotnet test
```
