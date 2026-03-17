# Short Code Algorithm

## Overview

Short codes are generated using **base62 encoding of a random value**. Collision freedom is ensured by the database unique constraint on `short_code`; the application retries with a new code if a duplicate is inserted.

## Algorithm

1. **Character set:** Base62 = `0-9`, `A-Z`, `a-z` (62 characters). This keeps codes URL-safe and compact.
2. **Length:** Fixed 8 characters (configurable in `ShortCodeGenerator`).
3. **Generation:** For each code, 8 random bytes are generated (`Random.Shared.NextBytes`). Each byte is mapped to one base62 character via `value % 62`, so the output is a 8-character alphanumeric string.
4. **Collision handling:** The `links` table has a unique index on `short_code`. When adding a link, if the insert fails due to duplicate key, the caller (e.g. create-link handler) generates a new code and retries. With 62^8 possibilities and random distribution, collisions are rare at typical volumes.

## Alternatives Considered

- **Base62 + counter:** Would require a persistent counter (e.g. DB sequence) and is more deterministic; we chose random for simplicity and to avoid a single write hotspot.
- **Random with collision check:** Implemented by retry on unique constraint violation rather than checking `ExistsAsync` before insert, to avoid race conditions.

## Implementation

- **Interface:** `ShortLink.Domain.IShortCodeGenerator` — `Task<string> GenerateAsync(CancellationToken)`.
- **Implementation:** `ShortLink.Infrastructure.Base62ShortCodeGenerator` (8 chars, random bytes → base62).
- **Persistence:** `ILinkRepository.AddAsync`; uniqueness enforced by unique index on `short_code` (table `links`).
