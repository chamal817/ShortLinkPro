## Story 1.2: Set up project and infrastructure

As a developer,  
I want the solution, API project, and runnable infrastructure (PostgreSQL, Redis) so that I can implement features against a real stack.

### Acceptance Criteria

- **Given** the architecture specifies .NET 8 Web API, PostgreSQL, and Redis  
  **When** I open the solution and run the API with dependencies  
  **Then** the API starts and can connect to PostgreSQL and Redis  
  **And** Docker Compose (or equivalent) brings up PostgreSQL and Redis with documented connection settings  
  **And** the solution and project layout follow the architecture (e.g. separate ShortLink.Api, ShortLink.Domain, and ShortLink.Infrastructure projects with clear boundaries)

### Implementation Plan

- **Solution structure**
  - Ensure solution has projects:
    - `ShortLink.Api` (presentation / endpoints).
    - `ShortLink.Domain` (entities, interfaces, domain services).
    - `ShortLink.Infrastructure` (EF Core DbContext, repositories, Redis cache).
  - Wire references:
    - `ShortLink.Api` references `ShortLink.Domain` and `ShortLink.Infrastructure`.
- **Database and Redis**
  - Add Docker Compose file defining:
    - PostgreSQL container with database/user/password.
    - Redis container.
  - Document environment variables and connection strings.
- **Configuration**
  - In `appsettings.json` and `appsettings.Development.json`, add:
    - `ConnectionStrings:Default` pointing to PostgreSQL.
    - `Redis:ConnectionString` for Redis.
- **EF Core**
  - In `ShortLink.Infrastructure`, add `AppDbContext` deriving from `DbContext`.
  - Register `AppDbContext` in `Program.cs` using `UseNpgsql`.
- **Connectivity validation**
  - On startup, ensure database migrations can be applied.
  - Optionally add a `/health/db` endpoint that checks DB connectivity.

### Sample Implementation Code (docker-compose.yml)

```yaml
version: "3.9"

services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: shortlink
      POSTGRES_USER: shortlink_user
      POSTGRES_PASSWORD: shortlink_password
    ports:
      - "5432:5432"

  redis:
    image: redis:7
    ports:
      - "6379:6379"
```

### Sample Implementation Code (Program.cs – infrastructure wiring)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
```

### Dev Agent Record

**Implemented:** Solution structure: `ShortLink.Api`, `ShortLink.Domain`, `ShortLink.Infrastructure`. Api references Domain and Infrastructure; Infrastructure references Domain. Domain: `Link` entity (Id, ShortCode, LongUrl, CreatedAt). Infrastructure: `AppDbContext` with `DbSet<Link>`, Npgsql 8.0.11, EF Core 8.0.11, `Microsoft.Extensions.Caching.StackExchangeRedis` 8.0.11; initial EF migration in `ShortLink.Infrastructure/Migrations`. Program.cs: `AddDbContext<AppDbContext>(UseNpgsql(ConnectionStrings:Default))`, `AddStackExchangeRedisCache(ConnectionStrings:Redis)`; on startup `Migrate()` when `IsRelational()` (skipped for in-memory in tests). Endpoints: `/health`, `/health/db` (DB connectivity). Docker Compose: `docker-compose.yml` with postgres:16 (DB shortlink, user shortlink_user, password shortlink_password) and redis:7; ports 5432, 6379. appsettings/appsettings.Development: `ConnectionStrings:Default`, `ConnectionStrings:Redis`. README updated: connection table and solution structure.

**Tests:** `HealthEndpointTests`: in-memory DbContext override via `ConfigureServices` (remove `DbContextOptions<AppDbContext>`, add `UseInMemoryDatabase("TestDb")`); 3 tests: `Get_Health_Returns200Ok`, `Get_Health_ReturnsJsonWithStatusOk`, `Get_HealthDb_Returns200WhenDatabaseConnected`. Test project: `Microsoft.EntityFrameworkCore.InMemory` 8.0.11, reference to `ShortLink.Infrastructure`. All tests pass.

**Files changed:** `ShortLink.sln` (Domain, Infrastructure added; nested under src), `src/ShortLink.Domain/ShortLink.Domain.csproj` (new), `src/ShortLink.Domain/Link.cs` (new), `src/ShortLink.Infrastructure/ShortLink.Infrastructure.csproj` (new), `src/ShortLink.Infrastructure/AppDbContext.cs` (new), `src/ShortLink.Infrastructure/Migrations/*` (new), `src/ShortLink.Api/ShortLink.Api.csproj` (refs Domain, Infrastructure; EF Design), `src/ShortLink.Api/Program.cs` (DbContext, Redis, Migrate, /health/db), `src/ShortLink.Api/appsettings.json`, `src/ShortLink.Api/appsettings.Development.json` (ConnectionStrings), `src/ShortLink.Api/Properties/launchSettings.json` (launchUrl swagger), `docker-compose.yml` (new), `README.md` (connection settings, solution structure, health URLs), `tests/ShortLink.Api.UnitTests/ShortLink.Api.UnitTests.csproj` (ref Infrastructure, EF InMemory), `tests/ShortLink.Api.UnitTests/HealthEndpointTests.cs` (in-memory override, /health/db test).

