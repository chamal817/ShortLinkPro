## Story 2.2: Implement GET /{shortCode} redirect with 302

As an end user,  
I want to open a short link and be redirected to the original URL so that I reach the intended content quickly.

### Acceptance Criteria

- **Given** a short code that exists in the system  
  **When** I request GET `/{shortCode}` (or the configured redirect route)  
  **Then** the server responds with HTTP 302 (or 301) and `Location` header set to the stored long URL  
  **And** the redirect is performed using the cache layer so that P95 latency can meet the \< 100ms target  
  **And** the response does not expose implementation details in the body (redirect only)

### Implementation Plan

- **Routing**
  - Add a minimal API endpoint:
    - Route template: `/{shortCode}`.
    - Constraint shortCode to expected pattern if desired (e.g. `[a-zA-Z0-9]+`).
- **Resolution**
  - Inject `ILinkResolver` into the endpoint.
  - Use `ResolveLongUrlAsync(shortCode)` to fetch the long URL (uses Redis + PostgreSQL as per Story 2.1).
- **Redirect**
  - For a found URL:
    - Return `Results.Redirect(longUrl, permanent: false)` for 302.
  - Ensure no implementation details are in the body.
- **Performance**
  - Ensure caching is in place and avoid additional DB calls when cache hit occurs.
  - Optional: add metrics for cache hit/miss and latency.

### Sample Implementation Code (Endpoint)

```csharp
app.MapGet("/{shortCode}", async (
    string shortCode,
    ILinkResolver linkResolver,
    CancellationToken cancellationToken) =>
{
    var longUrl = await linkResolver.ResolveLongUrlAsync(shortCode, cancellationToken);
    if (longUrl is null)
    {
        return Results.NotFound();
    }

    return Results.Redirect(longUrl, permanent: false);
})
.WithName("RedirectByShortCode")
.WithOpenApi(operation =>
{
    operation.Summary = "Redirect to the original URL for a given short code.";
    operation.Responses["302"]!.Description = "Redirect to original URL.";
    operation.Responses["404"]!.Description = "Short code not found.";
    return operation;
});
```

