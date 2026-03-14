# Epic 1: Create Short Link

Users and developers can create a short URL from a long URL via the API and receive a stable short link.

**FRs covered:** FR1, FR2, FR3, FR9, FR10, FR13.

---

## Story 1.1: Create environment and base application

As a developer,
I want a working development environment and a minimal runnable base application so that I can start building the ShortLink API on a solid foundation.

**Acceptance Criteria:**

**Given** the architecture specifies .NET 8 Web API  
**When** I set up the development environment and create the base application  
**Then** the .NET 8 SDK is available and the solution builds and runs  
**And** a Web API project exists (e.g. created with `dotnet new webapi` for ShortLink.Api)  
**And** the base API runs and returns a minimal response (e.g. default endpoint or a simple health/hello)  
**And** OpenAPI/Swagger is enabled so that the API is discoverable  
**And** a README or doc describes how to run the project locally (without Docker if desired)  
**And** the repository or folder structure is ready for adding Features, Domain, and Infrastructure in the next story

---

## Story 1.2: Set up project and infrastructure

As a developer,
I want the solution, API project, and runnable infrastructure (PostgreSQL, Redis) so that I can implement features against a real stack.

**Acceptance Criteria:**

**Given** the architecture specifies .NET 8 Web API, PostgreSQL, and Redis  
**When** I open the solution and run the API with dependencies  
**Then** the API starts and can connect to PostgreSQL and Redis  
**And** Docker Compose (or equivalent) brings up PostgreSQL and Redis with documented connection settings  
**And** the solution and project layout follow the architecture (e.g. separate ShortLink.Api, ShortLink.Domain, and ShortLink.Infrastructure projects with clear boundaries)

---

## Story 1.3: Persist links and generate unique short codes

As a developer,
I want the system to store link mappings and generate collision-free short codes so that each long URL gets a unique short link.

**Acceptance Criteria:**

**Given** a long URL is accepted for shortening  
**When** the system generates a short code and persists the mapping  
**Then** the short code is unique (no collisions with existing codes)  
**And** the mapping is stored in PostgreSQL (e.g. table with short_code, long_url, created_at)  
**And** schema is versioned via migrations (EF Core or SQL scripts)  
**And** the short code algorithm is documented (e.g. base62 + counter or random with collision check)

---

## Story 1.4: Expose POST /api/links create endpoint

As a client (developer or application),
I want to POST a long URL and receive a short URL so that I can share or embed short links.

**Acceptance Criteria:**

**Given** the API is running and the database is available  
**When** I send POST /api/links with a valid JSON body containing the long URL  
**Then** the system creates a new link and returns 201 with shortCode and shortUrl (full URL) in the response  
**And** invalid or missing long URL returns 400 with error body per architecture  
**And** the endpoint is documented in OpenAPI/Swagger  
**And** (optional) the new mapping is written to Redis so the first redirect can be fast

---

## Story 1.5: Implement sharded/partitioned PostgreSQL schema

As a developer,
I want the PostgreSQL schema for links (and click counts) to be sharded or partitioned by short_code so that the database can scale horizontally to handle 10M+ redirects per day.

**Acceptance Criteria:**

**Given** the architecture specifies a sharded/partitioned PostgreSQL backend keyed by short_code  
**When** I design and apply the database schema  
**Then** the links data is stored in a PostgreSQL schema that uses short_code as the sharding/partitioning key (e.g. hash or range partitioning)  
**And** a primary key or unique constraint exists on short_code across all partitions/shards  
**And** application queries that resolve a single short code continue to work without changing the API contract (sharding is transparent to callers)  
**And** migration/deployment steps for initializing and evolving the sharded/partitioned schema are documented

---

### Dev Agent Record (Story 1.4)

**Implemented:** POST `/api/links` minimal API in `Program.cs`. Request body: `CreateLinkRequest` (LongUrl). Response: 201 Created with `CreateLinkResponse` (ShortCode, ShortUrl); Location header `/api/links/{shortCode}`. Validation: required LongUrl, max length 2048, absolute HTTP/HTTPS URL; invalid or missing returns 400 with `ErrorResponse` (Error). Short code generated via `IShortCodeGenerator` with up to 5 retries on collision (`ILinkRepository.ExistsAsync`). Base URL from config `ShortLink:BaseUrl` (default `http://localhost:5000`). OpenAPI summary/description set via `WithOpenApi`. Redis cache for new mapping left optional and not implemented.

**Tests:** `tests/ShortLink.Api.UnitTests/CreateLinkEndpointTests.cs` — 6 tests: valid URL → 201 with ShortCode and ShortUrl; missing LongUrl → 400; empty LongUrl → 400; invalid scheme (ftp) → 400; invalid format → 400; valid URL → Location header present. Test host uses in-memory DB via `ConfigureTestServices` replacing `AppDbContext` with `UseInMemoryDatabase`.

**Files changed:** `src/ShortLink.Api/Program.cs`, `src/ShortLink.Api/appsettings.json`, `src/ShortLink.Api/appsettings.Development.json`, `tests/ShortLink.Api.UnitTests/CreateLinkEndpointTests.cs` (new).
