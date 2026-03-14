---
stepsCompleted: [1, 2, 3, 4]
inputDocuments: ['prd.md', 'architecture.md']
---

# URL-Shoter - Epic Breakdown

**Multi-file layout:** The same content is also available as multiple files for the SM agent (Sprint Planning, Context Story). See folder **`epics/`**: `epics/index.md` (requirements + epic list) and `epics/epic-1.md` … `epics/epic-4.md` (one file per epic with stories). Sprint planning supports both this single file and the `epics/*.md` folder.

## Overview

This document provides the complete epic and story breakdown for URL-Shoter (ShortLink), decomposing the requirements from the PRD and Architecture into implementable stories.

## Requirements Inventory

### Functional Requirements

- FR1: A client can submit a long URL to the API and receive a short URL (and short code).
- FR2: The system generates a unique short code for each accepted long URL (no collisions).
- FR3: The system stores the mapping between short code and long URL for at least 5 years (or until explicitly removed if such capability is added).
- FR4: A client can request resolution of a short code and receive an HTTP redirect to the stored long URL.
- FR5: The system resolves short codes in a way that supports P95 redirect latency < 100ms (with cache and infrastructure as specified).
- FR6: The system returns a standard HTTP error (e.g. 404) when the short code is unknown or invalid.
- FR7: The system increments a click count when a redirect is performed for a given short code.
- FR8: A client can retrieve the click count (and optionally the long URL) for a given short code via API or defined interface.
- FR9: The system exposes a documented API for creating short URLs and for resolving/redirecting (and optionally reading metadata).
- FR10: The system supports programmatic access without authentication in MVP (auth is out of scope for MVP).
- FR11: The system runs as stateless application servers so that instances can be added or removed behind a load balancer.
- FR12: The system uses a cache layer (Redis) for short-code lookups on the redirect path to meet latency and cache hit rate targets.
- FR13: The system persists short-code ↔ long-URL mappings and click counts in a durable store (PostgreSQL) with a design that supports future sharding if needed.
- FR14: The system can be deployed using Docker and a load-balanced API layer as specified.

### NonFunctional Requirements

- NFR-P1: P95 latency for redirect requests (short code → redirect response) shall be < 100ms.
- NFR-P2: Create-shorten API response time shall be within defined targets (e.g. P95 < 500ms) under normal load.
- NFR-P3: Cache hit rate for redirect lookups shall be > 80% under representative traffic.
- NFR-S1: The system shall support at least 10M redirect requests per day through horizontal scaling of API instances and cache capacity.
- NFR-S2: The system shall be designed so that adding application instances and cache nodes improves throughput (stateless API, cache layer, DB connection pooling).
- NFR-R1: Target system uptime shall be 99.9% (excluding planned maintenance).
- NFR-R2: Short URL mappings shall be retained for at least 5 years unless explicitly removed or a retention policy is introduced.
- NFR-O1: The system shall be deployable as Docker containers with documented dependencies (PostgreSQL, Redis).
- NFR-O2: The system shall expose sufficient observability (metrics, health checks, logs) to operate and validate SLAs.

### Additional Requirements

- .NET 8 Web API; initialize with `dotnet new webapi` (or equivalent). Project structure per architecture (Features, Domain, Infrastructure).
- PostgreSQL for links table (short_code, long_url, created_at) and click count (column or separate table); migrations (EF Core or SQL scripts).
- Redis for cache-aside on redirect path; key = short_code, value = long_url; optional TTL.
- REST endpoints: POST /api/links (create), GET /{shortCode} (redirect), GET /api/links/{shortCode} (metadata). OpenAPI/Swagger documentation.
- Error response format: JSON `{ "error": { "code": "...", "message": "..." } }`. HTTP status 400, 404, 429, 503 as specified in PRD.
- Naming: DB snake_case; API JSON camelCase; C# PascalCase/camelCase per architecture.
- Health checks: /health/live, /health/ready (DB + Redis). Observability: structured logs, metrics (redirect count, latency, cache hit rate).
- Load testing with k6 to validate P95 and throughput.

### FR Coverage Map

- FR1: Epic 1 – Create short URL via API
- FR2: Epic 1 – Unique short code generation
- FR3: Epic 1 – Persist mapping in PostgreSQL
- FR4: Epic 2 – Redirect resolution
- FR5: Epic 2 – Low-latency redirect (cache)
- FR6: Epic 2 – 404 for unknown short code
- FR7: Epic 3 – Increment click count on redirect
- FR8: Epic 3 – Metadata API for click count and long URL
- FR9: Epic 1, 2, 3 – Documented API across create, redirect, metadata
- FR10: Epic 1 – No auth in MVP
- FR11: Epic 4 – Stateless API (no in-memory state)
- FR12: Epic 2 – Redis cache layer
- FR13: Epic 1, 3 – PostgreSQL persistence
- FR14: Epic 4 – Docker and load-balanced deployment

## Epic List

### Epic 1: Create Short Link
Users and developers can create a short URL from a long URL via the API and receive a stable short link.
**FRs covered:** FR1, FR2, FR3, FR9, FR10, FR13.

### Epic 2: Redirect to Original URL
End users can click a short link and be redirected to the original URL with low latency; unknown links return 404.
**FRs covered:** FR4, FR5, FR6, FR12.

### Epic 3: Click Tracking & Metadata
Users can see how many times a short link was clicked and retrieve the original URL via the API.
**FRs covered:** FR7, FR8.

### Epic 4: Operations & Observability
Operators can deploy the full stack with Docker and monitor health, latency, and cache effectiveness.
**FRs covered:** FR11, FR14. **NFRs:** NFR-O1, NFR-O2.

---

## Epic 1: Create Short Link

Users and developers can create a short URL from a long URL via the API and receive a stable short link.

### Story 1.1: Create environment and base application

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

### Story 1.2: Set up project and infrastructure

As a developer,
I want the solution, API project, and runnable infrastructure (PostgreSQL, Redis) so that I can implement features against a real stack.

**Acceptance Criteria:**

**Given** the architecture specifies .NET 8 Web API, PostgreSQL, and Redis  
**When** I open the solution and run the API with dependencies  
**Then** the API starts and can connect to PostgreSQL and Redis  
**And** Docker Compose (or equivalent) brings up PostgreSQL and Redis with documented connection settings  
**And** the project layout follows the architecture (e.g. Features, Domain, Infrastructure folders)

### Story 1.3: Persist links and generate unique short codes

As a developer,
I want the system to store link mappings and generate collision-free short codes so that each long URL gets a unique short link.

**Acceptance Criteria:**

**Given** a long URL is accepted for shortening  
**When** the system generates a short code and persists the mapping  
**Then** the short code is unique (no collisions with existing codes)  
**And** the mapping is stored in PostgreSQL (e.g. table with short_code, long_url, created_at)  
**And** schema is versioned via migrations (EF Core or SQL scripts)  
**And** the short code algorithm is documented (e.g. base62 + counter or random with collision check)

### Story 1.4: Expose POST /api/links create endpoint

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

## Epic 2: Redirect to Original URL

End users can click a short link and be redirected to the original URL with low latency; unknown links return 404.

### Story 2.1: Implement Redis cache layer for short-code lookups

As a developer,
I want the redirect path to use Redis for short-code → long-URL lookups so that redirect latency stays low and cache hit rate can exceed 80%.

**Acceptance Criteria:**

**Given** Redis is configured and available  
**When** the application needs to resolve a short code to a long URL  
**Then** it checks Redis first (cache-aside)  
**And** on cache miss it reads from PostgreSQL and populates Redis for subsequent requests  
**And** cache key and value format follow the architecture (e.g. key = short_code, value = long_url)  
**And** the API remains stateless (no in-memory state between requests)

### Story 2.2: Implement GET /{shortCode} redirect with 302

As an end user,
I want to open a short link and be redirected to the original URL so that I reach the intended content quickly.

**Acceptance Criteria:**

**Given** a short code that exists in the system  
**When** I request GET /{shortCode} (or the configured redirect route)  
**Then** the server responds with HTTP 302 (or 301) and Location header set to the stored long URL  
**And** the redirect is performed using the cache layer so that P95 latency can meet the < 100ms target  
**And** the response does not expose implementation details in the body (redirect only)

### Story 2.3: Return 404 for unknown or invalid short codes

As an end user,
I want to receive a clear error when a short link does not exist so that I know the link is broken or wrong.

**Acceptance Criteria:**

**Given** a short code that is unknown or invalid  
**When** I request GET /{shortCode}  
**Then** the server responds with HTTP 404  
**And** the response body follows the architecture error format (e.g. JSON with error code and message)  
**And** no redirect is performed

---

## Epic 3: Click Tracking & Metadata

Users can see how many times a short link was clicked and retrieve the original URL via the API.

### Story 3.1: Increment click count on redirect

As a product or marketing user,
I want the system to count each redirect so that I can measure how often a short link is used.

**Acceptance Criteria:**

**Given** a redirect is performed for a valid short code  
**When** the server sends the 302 response  
**Then** the click count for that short code is incremented in persistent storage (PostgreSQL)  
**And** the increment may be synchronous or asynchronous; eventual consistency for the count is acceptable  
**And** redirect latency remains within target (cache and increment design support NFR-P1)

### Story 3.2: Expose GET /api/links/{shortCode} metadata endpoint

As a client (developer or dashboard),
I want to retrieve the long URL and click count for a short code so that I can display analytics or verify the link.

**Acceptance Criteria:**

**Given** a short code that exists in the system  
**When** I request GET /api/links/{shortCode}  
**Then** the server returns 200 with JSON containing longUrl and clickCount (and any other stored metadata)  
**And** if the short code is unknown, the server returns 404 with the standard error format  
**And** the endpoint is documented in OpenAPI/Swagger  
**And** no authentication is required in MVP

---

## Epic 4: Operations & Observability

Operators can deploy the full stack with Docker and monitor health, latency, and cache effectiveness.

### Story 4.1: Dockerfile and docker-compose for full stack

As an operator,
I want to run the API, PostgreSQL, and Redis with Docker so that I can deploy and scale the service as specified.

**Acceptance Criteria:**

**Given** Docker (and Docker Compose) is available  
**When** I run the provided Compose (or equivalent) configuration  
**Then** the API container, PostgreSQL, and Redis start and the API can connect to both  
**And** the API is built from a Dockerfile that follows the architecture  
**And** configuration is via environment variables or appsettings override (no secrets in image)  
**And** the setup is documented (e.g. README or architecture doc)

### Story 4.2: Health checks (liveness and readiness)

As an operator or load balancer,
I want health endpoints so that I can detect process liveness and dependency readiness.

**Acceptance Criteria:**

**Given** the API is running  
**When** I request /health/live (or the configured liveness path)  
**Then** the server returns 200 when the process is up  
**When** I request /health/ready (or the configured readiness path)  
**Then** the server returns 200 only when PostgreSQL and Redis are reachable  
**And** readiness returns 503 or non-200 when a dependency is unavailable  
**And** health endpoints are documented

### Story 4.3: Observability (logging and metrics)

As an operator,
I want structured logs and metrics so that I can validate SLAs and troubleshoot issues.

**Acceptance Criteria:**

**Given** the API is running and handling requests  
**Then** logs are structured (e.g. JSON or key-value) and include relevant context (e.g. short code, status, duration)  
**And** metrics (or equivalent) expose redirect count, redirect latency (e.g. P95), and cache hit rate (or cache hit/miss counts)  
**And** observability is documented so that operators can use it to validate NFR-O2 and NFR-P1/P3

---

## Final Validation Summary

- **FR coverage:** All FR1–FR14 are covered by the above epics and stories.
- **NFR alignment:** NFR-P1/P2/P3, NFR-S1/S2, NFR-R1/R2, NFR-O1/O2 are addressed in Epic 2 (cache, latency), Epic 1 & 3 (persistence), and Epic 4 (Docker, health, observability).
- **Epic independence:** Epic 1 delivers create; Epic 2 adds redirect (uses Epic 1 data); Epic 3 adds tracking and metadata (uses Epic 1 & 2); Epic 4 adds deployment and observability. No epic requires a later epic to function.
- **Story order:** Within each epic, stories can be implemented in sequence without forward dependencies.

This epic breakdown is ready for development. Use the SM agent’s **[CS] Context Story** to expand a story into full implementation context for the developer agent, or **[SP] Sprint Planning** to sequence work.
