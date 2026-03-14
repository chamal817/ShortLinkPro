# Epic 2: Redirect to Original URL

End users can click a short link and be redirected to the original URL with low latency; unknown links return 404.

**FRs covered:** FR4, FR5, FR6, FR12.

---

## Story 2.1: Implement Redis cache layer for short-code lookups

As a developer,
I want the redirect path to use Redis for short-code → long-URL lookups so that redirect latency stays low and cache hit rate can exceed 80%.

**Acceptance Criteria:**

**Given** Redis is configured and available  
**When** the application needs to resolve a short code to a long URL  
**Then** it checks Redis first (cache-aside)  
**And** on cache miss it reads from PostgreSQL and populates Redis for subsequent requests  
**And** cache key and value format follow the architecture (e.g. key = short_code, value = long_url)  
**And** the API remains stateless (no in-memory state between requests)

---

## Story 2.2: Implement GET /{shortCode} redirect with 302

As an end user,
I want to open a short link and be redirected to the original URL so that I reach the intended content quickly.

**Acceptance Criteria:**

**Given** a short code that exists in the system  
**When** I request GET /{shortCode} (or the configured redirect route)  
**Then** the server responds with HTTP 302 (or 301) and Location header set to the stored long URL  
**And** the redirect is performed using the cache layer so that P95 latency can meet the < 100ms target  
**And** the response does not expose implementation details in the body (redirect only)

---

## Story 2.3: Return 404 for unknown or invalid short codes

As an end user,
I want to receive a clear error when a short link does not exist so that I know the link is broken or wrong.

**Acceptance Criteria:**

**Given** a short code that is unknown or invalid  
**When** I request GET /{shortCode}  
**Then** the server responds with HTTP 404  
**And** the response body follows the architecture error format (e.g. JSON with error code and message)  
**And** no redirect is performed
