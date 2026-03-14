## Story 3.1: Increment click count on redirect

As a product or marketing user,  
I want the system to count each redirect so that I can measure how often a short link is used.

### Acceptance Criteria

- **Given** a redirect is performed for a valid short code  
  **When** the server sends the 302 response  
  **Then** the click count for that short code is incremented in persistent storage (PostgreSQL)  
  **And** the increment may be synchronous or asynchronous; eventual consistency for the count is acceptable  
  **And** redirect latency remains within target (cache and increment design support NFR-P1)

### Implementation Plan

- **Data model**
  - Add `ClickCount` column to `links` table, or introduce a separate `link_clicks` table keyed by `link_id` or `short_code`.
  - Ensure writes are cheap (single `UPDATE` or `INSERT ... ON CONFLICT`).
- **Increment strategy**
  - Start with a simple synchronous increment in the redirect flow:
    - After resolving the long URL and before returning the redirect, increment click count.
  - Optionally refactor later to an asynchronous pipeline (e.g. background queue) if needed for throughput.
- **Repository abstraction**
  - Extend `ILinkRepository` with:
    - `Task IncrementClickCountAsync(string shortCode, CancellationToken)`.
- **Redirect endpoint integration**
  - In the `GET /{shortCode}` endpoint:
    - On successful resolution (URL found), call `IncrementClickCountAsync` and then return the redirect.
  - Ensure failures to increment do not block the redirect (best-effort logging).

### Sample Implementation Code (schema change – single table)

```sql
ALTER TABLE links
ADD COLUMN IF NOT EXISTS click_count bigint NOT NULL DEFAULT 0;
```

### Sample Implementation Code (repository and redirect)

```csharp
public interface ILinkRepository
{
    Task<Link?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);
    Task IncrementClickCountAsync(string shortCode, CancellationToken cancellationToken = default);
}

app.MapGet("/{shortCode}", async (
    string shortCode,
    ILinkResolver linkResolver,
    ILinkRepository linkRepository,
    CancellationToken cancellationToken) =>
{
    var longUrl = await linkResolver.ResolveLongUrlAsync(shortCode, cancellationToken);
    if (longUrl is null)
    {
        return Results.NotFound(new ErrorResponse("Short code not found."));
    }

    try
    {
        await linkRepository.IncrementClickCountAsync(shortCode, cancellationToken);
    }
    catch
    {
        // Log and ignore to avoid impacting redirect latency.
    }

    return Results.Redirect(longUrl, permanent: false);
});
```

