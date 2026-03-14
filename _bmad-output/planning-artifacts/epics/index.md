---
stepsCompleted: [1, 2, 3, 4]
inputDocuments: ['prd.md', 'architecture.md']
---

# URL-Shoter - Epic Breakdown (Index)

## Overview

This folder contains the epic and story breakdown for URL-Shoter (ShortLink) in **multiple files**. The SM agent (Sprint Planning, Context Story) supports both this layout and a single `epics.md` file.

- **This file:** Requirements inventory, FR coverage map, epic list.
- **Story details:** [epic-1.md](epic-1.md) | [epic-2.md](epic-2.md) | [epic-3.md](epic-3.md) | [epic-4.md](epic-4.md)

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

- .NET 8 Web API; project structure per architecture (Features, Domain, Infrastructure).
- PostgreSQL for links and click count; Redis for cache-aside; REST endpoints and OpenAPI; error format and naming per architecture.
- Health checks and observability; load testing with k6.

### FR Coverage Map

- FR1–FR3, FR9, FR10, FR13: Epic 1
- FR4–FR6, FR12: Epic 2
- FR7–FR8: Epic 3
- FR11, FR14: Epic 4

## Epic List

| Epic | Title | Stories | FRs |
|------|--------|---------|-----|
| 1 | [Create Short Link](epic-1.md) | 1.1, 1.2, 1.3, 1.4 | FR1, FR2, FR3, FR9, FR10, FR13 |
| 2 | [Redirect to Original URL](epic-2.md) | 2.1, 2.2, 2.3 | FR4, FR5, FR6, FR12 |
| 3 | [Click Tracking & Metadata](epic-3.md) | 3.1, 3.2 | FR7, FR8 |
| 4 | [Operations & Observability](epic-4.md) | 4.1, 4.2, 4.3 | FR11, FR14 |

Sprint Planning and Context Story can use either this folder (`epics/*.md`) or the single file `../epics.md`.
