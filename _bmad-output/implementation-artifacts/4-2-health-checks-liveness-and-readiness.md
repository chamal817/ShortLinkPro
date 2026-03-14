## Story 4.2: Health checks (liveness and readiness)

As an operator or load balancer,  
I want health endpoints so that I can detect process liveness and dependency readiness.

### Acceptance Criteria

- **Given** the API is running  
  **When** I request `/health/live` (or the configured liveness path)  
  **Then** the server returns 200 when the process is up  
  **When** I request `/health/ready` (or the configured readiness path)  
  **Then** the server returns 200 only when PostgreSQL and Redis are reachable  
  **And** readiness returns 503 or non-200 when a dependency is unavailable  
  **And** health endpoints are documented

### Implementation Plan

- **Use ASP.NET Core HealthChecks**
  - Add health check services:
    - Basic liveness check (no dependencies).
    - Readiness check that verifies:
      - PostgreSQL connection (e.g. `AddNpgSql`).
      - Redis connectivity.
- **Endpoints**
  - Map:
    - `/health/live` → liveness-only check (no external dependency checks).
    - `/health/ready` → includes DB and Redis checks.
- **Status codes**
  - Configure health check options so:
    - Healthy → 200.
    - Degraded/Unhealthy → 503.
- **Documentation**
  - Add to README or API docs the purpose and usage of both endpoints.

### Sample Implementation Code (Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);

// ... existing service registrations ...

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddNpgSql(
        builder.Configuration.GetConnectionString("Default")!,
        name: "postgres",
        tags: new[] { "ready" })
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis")!,
        name: "redis",
        tags: new[] { "ready" });

var app = builder.Build();

// ... existing middleware ...

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});
```

