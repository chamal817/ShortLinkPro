## Story 4.1: Dockerfile and docker-compose for full stack

As an operator,  
I want to run the API, PostgreSQL, and Redis with Docker so that I can deploy and scale the service as specified.

### Acceptance Criteria

- **Given** Docker (and Docker Compose) is available  
  **When** I run the provided Compose (or equivalent) configuration  
  **Then** the API container, PostgreSQL (single or sharded/partitioned as configured), and Redis start and the API can connect to all required instances  
  **And** the API is built from a Dockerfile that follows the architecture  
  **And** configuration is via environment variables or appsettings override (no secrets in image)  
  **And** the setup is documented (e.g. README or architecture doc)

### Implementation Plan

- **API Dockerfile**
  - Use multi-stage build:
    - `sdk` stage: restore, build, publish.
    - `aspnet` runtime stage: copy published output.
  - Expose appropriate port (e.g. 8080).
  - Use environment variables for:
    - `ConnectionStrings__Default`
    - `Redis__ConnectionString`
    - `ShortLink__BaseUrl`
- **docker-compose.yml**
  - Define services:
    - `api`: build from Dockerfile, depends on `postgres` and `redis`.
    - `postgres`: standard Postgres image with DB/user/password env vars and volume.
    - `redis`: Redis image with exposed port.
  - Configure `api` service environment to point to `postgres` and `redis` hostnames.
  - Map host ports for development/testing.
- **Documentation**
  - Add section to README describing:
    - How to build and run: `docker compose up --build`.
    - Default URLs and credentials.

### Sample Implementation Code (Dockerfile)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ./src ./src
COPY ./ShortLink.sln .

RUN dotnet restore
RUN dotnet publish ./src/ShortLink.Api/ShortLink.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ShortLink.Api.dll"]
```

### Sample Implementation Code (docker-compose.yml)

```yaml
version: "3.9"

services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    depends_on:
      - postgres
      - redis
    environment:
      ASPNETCORE_ENVIRONMENT: "Production"
      ConnectionStrings__Default: "Host=postgres;Port=5432;Database=shortlink;Username=shortlink_user;Password=shortlink_password"
      Redis__ConnectionString: "redis:6379"
      ShortLink__BaseUrl: "http://localhost:8080"
    ports:
      - "8080:8080"

  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: shortlink
      POSTGRES_USER: shortlink_user
      POSTGRES_PASSWORD: shortlink_password
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7
    ports:
      - "6379:6379"

volumes:
  postgres_data:
```

