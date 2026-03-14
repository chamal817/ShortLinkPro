# Epic 3: Click Tracking & Metadata

Users can see how many times a short link was clicked and retrieve the original URL via the API.

**FRs covered:** FR7, FR8.

---

## Story 3.1: Increment click count on redirect

As a product or marketing user,
I want the system to count each redirect so that I can measure how often a short link is used.

**Acceptance Criteria:**

**Given** a redirect is performed for a valid short code  
**When** the server sends the 302 response  
**Then** the click count for that short code is incremented in persistent storage (PostgreSQL)  
**And** the increment may be synchronous or asynchronous; eventual consistency for the count is acceptable  
**And** redirect latency remains within target (cache and increment design support NFR-P1)

---

## Story 3.2: Expose GET /api/links/{shortCode} metadata endpoint

As a client (developer or dashboard),
I want to retrieve the long URL and click count for a short code so that I can display analytics or verify the link.

**Acceptance Criteria:**

**Given** a short code that exists in the system  
**When** I request GET /api/links/{shortCode}  
**Then** the server returns 200 with JSON containing longUrl and clickCount (and any other stored metadata)  
**And** if the short code is unknown, the server returns 404 with the standard error format  
**And** the endpoint is documented in OpenAPI/Swagger  
**And** no authentication is required in MVP
