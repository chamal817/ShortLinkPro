---
stepsCompleted: ['step-01-init', 'step-02-discovery', 'step-02b-vision', 'step-02c-executive-summary', 'step-03-success', 'step-04-journeys', 'step-05-domain', 'step-06-innovation', 'step-07-project-type', 'step-08-scoping', 'step-09-functional', 'step-10-nonfunctional', 'step-11-polish', 'step-12-complete']
inputDocuments: ['product-brief-URL-Shoter-2026-03-11.md']
documentCounts:
  briefCount: 1
  researchCount: 0
  brainstormingCount: 0
  projectDocsCount: 0
workflowType: prd
classification:
  projectType: api_backend
  domain: developer_services
  complexity: low
  projectContext: greenfield
---

# Product Requirements Document - URL-Shoter

**Product name:** ShortLink — High Scale URL Shortener  
**Author:** User  
**Date:** 2026-03-07  

---

## Executive Summary

ShortLink is a high-scale URL shortening service that converts long URLs into short, shareable links and redirects users with minimal latency. The system targets 10M+ redirects per day, 99.9% availability, and sub-100ms P95 redirect latency by combining a Redis cache layer with a sharded PostgreSQL backend. It serves developers, social media users, marketing teams, and applications that need fast, reliable short links with a simple, API-first offering. The product addresses the shareability, trust, scale, and persistence problems of long URLs without mandatory authentication, custom domains, or billing.

### What Makes This Special

- **Focused scope:** Core shortening, redirect, and basic click tracking only—reducing integration time and cost for teams that do not need auth or advanced analytics.
- **Production-grade scale commitments:** 10M+ daily redirects, 80%+ cache hit rate, 5-year URL retention, and measurable latency/availability targets.
- **API-first design:** Stateless, horizontally scalable API with Redis caching and sharded PostgreSQL persistence (partitioned by short_code), suitable for programmatic and human use.

## Project Classification

| Attribute | Value |
|-----------|--------|
| **Project type** | API / Backend service |
| **Domain** | Developer services / infrastructure |
| **Complexity** | Low |
| **Project context** | Greenfield |

---

## Success Criteria

### User Success

- Users receive a short URL within a defined response time after submitting a long URL.
- Recipients are redirected to the original URL with P95 latency < 100ms.
- Short links remain valid and redirect correctly for at least 5 years.
- Users can obtain basic click counts per short link where the feature is exposed.

### Business Success

- Deliver a production-grade, high-scale URL shortener that meets stated latency and availability targets.
- Enable horizontal scalability and high cache effectiveness to support growth without proportional cost increase.
- Establish measurable SLAs (latency, uptime, retention) that customers can rely on.

### Technical Success

- P95 redirect latency < 100ms under load.
- System supports 10M+ redirects per day (design and/or load test).
- Cache hit rate > 80% with representative traffic.
- All core features deployable with Docker; stateless app servers behind a load-balanced API layer.

### Measurable Outcomes

| Metric | Target |
|--------|--------|
| P95 redirect latency | < 100 ms |
| Daily redirect capacity | 10M+ |
| Cache hit rate | > 80% |
| System uptime | 99.9% |
| URL retention | 5 years |

## Product Scope

### MVP - Minimum Viable Product

- URL shortening (accept long URL, return short link).
- Instant HTTP redirect from short code to original URL.
- Unique, collision-free short code generation.
- Basic click tracking (count redirects per short link; storage and optional read API).
- Scalable infrastructure: stateless API, Redis cache, PostgreSQL, Docker, load-balanced API layer.

### Growth Features (Post-MVP)

- Optional user authentication.
- Custom domains for branded short links.
- Richer analytics and dashboards.
- Rate limiting and abuse prevention.

### Vision (Future)

- Multi-region deployment for lower latency and higher availability.
- Advanced analytics and reporting.
- Billing or paid plans if business model evolves.

---

## User Journeys

### Journey 1: Developer shortens a link via API

Alex is integrating ShortLink into a docs site. They call the create endpoint with a long URL, receive a short URL, and embed it in the product. When visitors click the link, they are redirected immediately to the docs. Alex values a simple API, fast response on create, and reliable redirects so links never break.

**Requirements revealed:** Create-shorten API, stable short codes, redirect endpoint, API documentation.

### Journey 2: End user clicks a short link

Sam receives a short link in a message. They click it and are taken to the original URL with no noticeable delay. The link continues to work when revisited days or months later. If the link were broken or slow, trust in the sender would drop.

**Requirements revealed:** Resolve short code to long URL, HTTP redirect (302/301), low redirect latency, long-term URL retention.

### Journey 3: Marketing team checks click counts

Jordan creates short links for a campaign and shares them. Later they call an API (or use a minimal UI) to see how many times each link was clicked. They use this to compare channels and decide where to focus.

**Requirements revealed:** Increment click count on redirect, store count per short link, API (or UI) to read click counts.

### Journey 4: Operations runs and scales the service

Ops deploys ShortLink with Docker, puts a load balancer in front, and scales API instances as traffic grows. They monitor latency, cache hit rate, and errors to meet SLAs. When traffic spikes, adding instances and cache capacity keeps performance within targets.

**Requirements revealed:** Stateless API, horizontal scaling, Redis cache, observability (metrics/logs), load-balanced entry point.

### Journey Requirements Summary

| Journey | Capabilities |
|---------|--------------|
| Developer / API | Create short URL, read short URL metadata (e.g. click count), API contract and docs |
| End user (redirect) | Resolve short code, HTTP redirect, low latency, 5-year retention |
| Marketing / tracking | Click count per link, read counts via API or minimal UI |
| Operations | Deploy (Docker), scale (stateless + load balancer), cache (Redis), monitor (metrics) |

---

## Domain-Specific Requirements

No additional domain-specific compliance or regulatory requirements for MVP. The product operates in developer services with low complexity; standard API and infrastructure practices apply (data persistence, availability, latency as stated in NFRs).

---

## API-Specific Requirements

### Project-Type Overview

ShortLink is an API-first backend service. Primary interactions are HTTP API calls to create short URLs, resolve/redirect, and optionally read metadata (e.g. click counts). No user authentication in MVP; no custom domains or advanced analytics in MVP.

### Technical Architecture Considerations

- **Stateless application servers:** All request state in cache or database; no sticky sessions required.
- **Horizontal scalability:** Add API instances behind a load balancer to increase throughput.
- **Redis caching layer:** Cache short-code → long-URL lookups to achieve < 100ms P95 and > 80% cache hit rate, minimizing database reads on the redirect path.
- **PostgreSQL:** Sharded/partitioned persistent store for short-code ↔ long-URL mapping and click counts, using short_code as the shard/partition key to support horizontal growth.
- **Load-balanced API layer:** Single entry point for create and resolve traffic.

### Endpoint Specifications (MVP)

- **POST** (or equivalent) **Create short URL:** Request body contains long URL; response contains short URL (and short code). Idempotency or duplicate handling as defined (e.g. same long URL may or may not return same short URL).
- **GET Redirect:** Given short code (path or query), respond with HTTP redirect (302 or 301) to the stored long URL; increment click count when redirect is performed.
- **GET Read metadata (optional):** Given short code, return stored long URL and click count (or equivalent). No auth in MVP.

### Data Schemas

- **Create request:** Long URL (required); optional fields as needed (e.g. TTL if introduced later).
- **Create response:** Short URL (full URL), short code.
- **Resolve:** Short code in path or query; response is redirect only.
- **Metadata response:** Long URL, click count (and any other stored fields).

### Error Handling

- **400 Bad Request:** Invalid or missing long URL on create.
- **404 Not Found:** Short code unknown or expired (if TTL exists).
- **429 Too Many Requests:** Rate limiting (if implemented in MVP).
- **503 Service Unavailable:** Dependency (e.g. DB/cache) unavailable; retry or fallback as defined.

### Rate Limits and API Docs

- Rate limits: Define per-client or per-IP limits for create and redirect to protect the service; exact values and policy in implementation.
- API documentation: Machine- and human-readable (e.g. OpenAPI/Swagger) describing create, redirect, and metadata endpoints, request/response shapes, and error codes.

### Implementation Considerations

- Technology stack: .NET 8 Web API, PostgreSQL, Redis, Docker. Load testing with k6 to validate latency and throughput targets.

---

## Project Scoping & Phased Development

### MVP Strategy & Philosophy

**MVP approach:** Problem-solving MVP—minimum set of capabilities that deliver core value: shorten a URL, redirect reliably with low latency, and provide basic click counts. No auth, custom domains, or billing.

**Resource assumptions:** Small team; single deployment region; Docker-based deployment; PostgreSQL and Redis as specified.

### MVP Feature Set (Phase 1)

**Core user journeys supported:** Developer creates short link via API; end user clicks and is redirected; marketing/ops reads click counts; operations deploys and scales the service.

**Must-have capabilities:**

- Create short URL (API).
- Resolve short code and redirect (HTTP 302/301).
- Unique short code generation (collision-free).
- Persist mapping and click count (PostgreSQL).
- Cache lookups (Redis) for redirect path.
- Docker-based runnable deployment.
- Load-balanced, stateless API layer.

### Post-MVP Features

**Phase 2 (Growth):** Optional authentication, custom domains, richer analytics, rate limiting and abuse prevention.

**Phase 3 (Expansion):** Multi-region deployment, advanced analytics, billing/paid plans if required.

### Risk Mitigation Strategy

- **Technical:** Validate latency and throughput with k6; design DB and cache for 10M+ redirects/day; avoid single points of failure (e.g. multiple API instances, cache replica).
- **Market/usage:** MVP scope is narrow; iterate based on usage and feedback.
- **Resource:** Stateless design allows scaling with additional instances and cache capacity.

---

## Functional Requirements

### URL Shortening

- FR1: A client can submit a long URL to the API and receive a short URL (and short code).
- FR2: The system generates a unique short code for each accepted long URL (no collisions).
- FR3: The system stores the mapping between short code and long URL for at least 5 years (or until explicitly removed if such capability is added).

### Redirection

- FR4: A client can request resolution of a short code and receive an HTTP redirect to the stored long URL.
- FR5: The system resolves short codes in a way that supports P95 redirect latency < 100ms (with cache and infrastructure as specified).
- FR6: The system returns a standard HTTP error (e.g. 404) when the short code is unknown or invalid.

### Click Tracking

- FR7: The system increments a click count when a redirect is performed for a given short code.
- FR8: A client can retrieve the click count (and optionally the long URL) for a given short code via API or defined interface.

### API and Integration

- FR9: The system exposes a documented API for creating short URLs and for resolving/redirecting (and optionally reading metadata).
- FR10: The system supports programmatic access without authentication in MVP (auth is out of scope for MVP).

### Operations and Scalability

- FR11: The system runs as stateless application servers so that instances can be added or removed behind a load balancer.
- FR12: The system uses a cache layer (Redis) for short-code lookups on the redirect path to meet latency and cache hit rate targets.
- FR13: The system persists short-code ↔ long-URL mappings and click counts in a durable, sharded PostgreSQL store, with short_code used as the shard/partition key for horizontal scalability.
- FR14: The system can be deployed using Docker and a load-balanced API layer as specified.

---

## Non-Functional Requirements

### Performance

- **NFR-P1:** P95 latency for redirect requests (short code → redirect response) shall be < 100ms.
- **NFR-P2:** Create-shorten API response time shall be within defined targets (e.g. P95 < 500ms or as defined in implementation) under normal load.
- **NFR-P3:** Cache hit rate for redirect lookups shall be > 80% under representative traffic.

### Scalability

- **NFR-S1:** The system shall support at least 10M redirect requests per day through horizontal scaling of API instances and cache capacity.
- **NFR-S2:** The system shall be designed so that adding application instances and cache nodes improves throughput without single-bottleneck design (stateless API, cache layer, DB connection pooling as appropriate).

### Reliability and Availability

- **NFR-R1:** Target system uptime shall be 99.9% (excluding planned maintenance).
- **NFR-R2:** Short URL mappings shall be retained for at least 5 years unless explicitly removed or a retention policy is introduced and documented.

### Operational

- **NFR-O1:** The system shall be deployable as Docker containers with documented dependencies (PostgreSQL, Redis).
- **NFR-O2:** The system shall expose sufficient observability (e.g. metrics, health checks, logs) to operate and validate SLAs (latency, errors, cache hit rate).

---

This PRD defines the product and technical requirements for ShortLink (URL-Shoter). All design, architecture, and development work should trace back to this document. Update the PRD as scope or decisions change.
