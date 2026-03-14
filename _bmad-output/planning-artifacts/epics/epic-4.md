# Epic 4: Operations & Observability

Operators can deploy the full stack with Docker and monitor health, latency, and cache effectiveness.

**FRs covered:** FR11, FR14. **NFRs:** NFR-O1, NFR-O2.

---

## Story 4.1: Dockerfile and docker-compose for full stack

As an operator,
I want to run the API, PostgreSQL, and Redis with Docker so that I can deploy and scale the service as specified.

**Acceptance Criteria:**

**Given** Docker (and Docker Compose) is available  
**When** I run the provided Compose (or equivalent) configuration  
**Then** the API container, PostgreSQL (single or sharded/partitioned as configured), and Redis start and the API can connect to all required instances  
**And** the API is built from a Dockerfile that follows the architecture  
**And** configuration is via environment variables or appsettings override (no secrets in image)  
**And** the setup is documented (e.g. README or architecture doc)

---

## Story 4.2: Health checks (liveness and readiness)

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

---

## Story 4.3: Observability (logging and metrics)

As an operator,
I want structured logs and metrics so that I can validate SLAs and troubleshoot issues.

**Acceptance Criteria:**

**Given** the API is running and handling requests  
**Then** logs are structured (e.g. JSON or key-value) and include relevant context (e.g. short code, status, duration)  
**And** metrics (or equivalent) expose redirect count, redirect latency (e.g. P95), and cache hit rate (or cache hit/miss counts)  
**And** observability is documented so that operators can use it to validate NFR-O2 and NFR-P1/P3

---

## Story 4.4: k6 load tests for redirect and create endpoints

As an operator or performance engineer,
I want k6 load tests for the redirect and create endpoints so that I can validate latency and throughput targets (10M+ redirects/day, P95 < 100ms, cache hit rate > 80%).

**Acceptance Criteria:**

**Given** the API and its dependencies (PostgreSQL, Redis) are running in a test or staging environment  
**When** I run the provided k6 scripts for the redirect path and the create endpoint  
**Then** there are k6 test files (e.g. `load/k6/redirect.js` and `load/k6/create.js`) that generate representative traffic against GET /{shortCode} and POST /api/links  
**And** the scripts define thresholds that reflect PRD NFRs (e.g. P95 redirect latency < 100ms, acceptable error rate)  
**And** the tests can be parameterized for target RPS and environment URLs  
**And** documentation explains how to run the k6 tests and interpret results to decide whether NFRs are being met
