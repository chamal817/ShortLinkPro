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

| Service    | Host     | Port | User     | Password        | Database  |
|-----------|----------|------|----------|-----------------|----------|
| PostgreSQL | localhost | 5432 | shortlink | shortlink_secret | shortlink |
| Redis      | localhost | 6379 | —        | —               | —        |

- **PostgreSQL connection string:**  
  `Host=localhost;Port=5432;Database=shortlink;Username=shortlink;Password=shortlink_secret`
- **Redis configuration:**  
  `localhost:6379`

Override via environment: `ConnectionStrings__PostgreSQL`, `Redis__Configuration`.

## Run locally (without Docker for the API)

**Important:** Start PostgreSQL and Redis first, or the API will fail at startup with a connection error. From the repository root:

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
   - API: http://localhost:5152
   - Swagger UI: http://localhost:5152/swagger
   - Minimal endpoint: http://localhost:5152/hello
   - Health: http://localhost:5152/health/live (process), http://localhost:5152/health/ready (DB + Redis)

The API runs in development mode with Swagger enabled by default. If you see "Cannot connect to PostgreSQL", run `docker compose up -d` from the repo root and try again.

## Solution structure

- `ShortLink.sln` — solution file
- `src/ShortLink.Api/` — main Web API project (Features, Domain, Infrastructure layout for upcoming stories)
- `tests/` — unit and integration tests

## License

See repository license.
