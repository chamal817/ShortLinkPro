# ShortLink — URL Shortener API

High-scale URL shortener API built with .NET 8. This repo contains the ShortLink API and (in later stories) PostgreSQL persistence and Redis cache.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- For full stack (API + DB + cache): [Docker](https://www.docker.com/get-started) (optional)

## Infrastructure (PostgreSQL + Redis)

Docker Compose brings up PostgreSQL and Redis for local development:

```bash
docker compose up -d
```

**Connection settings** (used by the API via `appsettings` or environment variables):

| Service    | Host     | Port | User           | Password           | Database  |
|-----------|----------|------|----------------|--------------------|----------|
| PostgreSQL | localhost | 5432 | shortlink_user | shortlink_password | shortlink |
| Redis      | localhost | 6379 | —              | —                  | —        |

- **PostgreSQL connection string** (`ConnectionStrings:Default`):  
  `Host=localhost;Port=5432;Database=shortlink;Username=shortlink_user;Password=shortlink_password`
- **Redis** (`ConnectionStrings:Redis`):  
  `localhost:6379`

Override via environment: `ConnectionStrings__Default`, `ConnectionStrings__Redis`.

## Run locally (without Docker for the API)

**Base application (Story 1.1):** You can run the API with only the .NET 8 SDK—no Docker required. The `/health` endpoint returns `200 OK` and Swagger UI is available in Development. See commands below.

**Full stack:** Start PostgreSQL and Redis first, or the API will fail at startup with a connection error. From the repository root:

```bash
docker compose up -d
```

1. **Restore and build**
   ```bash
   dotnet restore
   dotnet build
   ```

2. **Run the API**
   ```bash
   dotnet run --project src/ShortLink.Api
   ```
   Or from the project directory:
   ```bash
   cd src/ShortLink.Api
   dotnet run
   ```

3. **Open in browser**
   - API: http://localhost:5067 (or port in `launchSettings.json`)
   - Swagger UI: http://localhost:5067/swagger
   - Health: http://localhost:5067/health — http://localhost:5067/health/db (DB connectivity)

The API runs in development mode with Swagger enabled by default. If you see "Cannot connect to PostgreSQL", run `docker compose up -d` from the repo root and try again.

## Run full stack with Docker (API + PostgreSQL + Redis)

From the repository root:

```bash
docker compose up --build
```

This will build the API image using the root `Dockerfile` and start:

- `api` (ShortLink.Api)
- `postgres` (PostgreSQL 16)
- `redis` (Redis 7)

**Default connection settings inside the Docker network** (used by the API via environment variables):

| Service     | Host     | Port | User           | Password           | Database  |
|------------|----------|------|----------------|--------------------|-----------|
| PostgreSQL | postgres | 5432 | shortlink_user | shortlink_password | shortlink |
| Redis      | redis    | 6379 | —              | —                  | —         |

The Compose file configures:

- `ConnectionStrings__Default=Host=postgres;Port=5432;Database=shortlink;Username=shortlink_user;Password=shortlink_password`
- `ConnectionStrings__Redis=redis:6379`
- `ShortLink__BaseUrl=http://localhost:8080`

**Host access:**

- API: http://localhost:8080
- Health (overall): http://localhost:8080/health
- Health (liveness): http://localhost:8080/health/live
- Health (readiness: DB + Redis): http://localhost:8080/health/ready

## Observability (logging and metrics)

- **Logging**: The API uses ASP.NET Core structured logging and writes logs to the console. Key events logged:
  - Link creation requests and validation failures.
  - Redirects (short code, outcome, duration).
  - Cache failures and database connection issues.

- **Metrics** (Prometheus):
  - HTTP metrics and application metrics are exposed on `/metrics`.
  - Custom metrics include:
    - `shortlink_redirect_total{status, cache_hit}` — total redirects by HTTP status and cache hit flag.
    - `shortlink_redirect_duration_seconds` — histogram of redirect latency.
    - `shortlink_cache_hit_total{operation}` / `shortlink_cache_miss_total{operation}` — cache hit/miss counters.

Use a Prometheus server or compatible scraper to collect `/metrics` and build dashboards/alerts for SLAs (availability, latency, and cache effectiveness).

To stop the stack:

```bash
docker compose down
```

## Troubleshooting

### `Npgsql.PostgresException: 28P01: password authentication failed for user "shortlink_user"`

This means the API is connecting to a PostgreSQL instance that does not have the `shortlink_user` / `shortlink_password` credentials (or is rejecting them).

- **If you intend to use the project’s Docker Postgres:**  
  Start the stack so the correct database is listening on port 5432:
  ```bash
  docker compose up -d
  ```
  Ensure no other PostgreSQL is bound to 5432 on localhost (e.g. a local install or another container). If something else uses 5432, either stop it or change the Compose port mapping (e.g. `"5433:5432"`) and set the connection string to use port 5433 (e.g. via `ConnectionStrings__Default` or in appsettings).

- **If you use your own PostgreSQL** (different port or local install):  
  Either create the user and database to match the app:
  ```sql
  CREATE USER shortlink_user WITH PASSWORD 'shortlink_password';
  CREATE DATABASE shortlink OWNER shortlink_user;
  ```
  Or override the connection string to use your existing user and password, for example:
  ```bash
  set ConnectionStrings__Default=Host=localhost;Port=5432;Database=shortlink;Username=YOUR_USER;Password=YOUR_PASSWORD
  dotnet run --project src/ShortLink.Api
  ```
  (On macOS/Linux use `export ConnectionStrings__Default=...`.)

## Solution structure

- `ShortLink.sln` — solution file
- `src/ShortLink.Api/` — Web API (endpoints, presentation)
- `src/ShortLink.Domain/` — entities and domain interfaces
- `src/ShortLink.Infrastructure/` — EF Core `AppDbContext`, repositories, Redis cache
- `tests/` — unit and integration tests

## License

See repository license.
