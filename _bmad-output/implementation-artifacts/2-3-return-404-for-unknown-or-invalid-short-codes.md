## Story 2.3: Return 404 for unknown or invalid short codes

As an end user,  
I want to receive a clear error when a short link does not exist so that I know the link is broken or wrong.

### Acceptance Criteria

- **Given** a short code that is unknown or invalid  
  **When** I request GET `/{shortCode}`  
  **Then** the server responds with HTTP 404  
  **And** the response body follows the architecture error format (e.g. JSON with error code and message)  
  **And** no redirect is performed

### Implementation Plan

- **Validation**
  - Optionally validate `shortCode` against allowed characters/length:
    - If invalid format, return 404 (or 400 if architecture requires).
- **Unknown codes**
  - Use `ILinkResolver`:
    - If `ResolveLongUrlAsync` returns `null`, treat as "not found".
- **Error response shape**
  - Reuse `ErrorResponse` (or central error contract) for consistency.
- **Behavior**
  - For unknown or invalid codes:
    - Do **not** call `Results.Redirect`.
    - Return 404 with error body.

### Sample Implementation Code (Enhanced redirect endpoint)

```csharp
public record ErrorResponse(string Error);

app.MapGet("/{shortCode}", async (
    string shortCode,
    ILinkResolver linkResolver,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(shortCode))
    {
        return Results.NotFound(new ErrorResponse("Short code not found."));
    }

    var longUrl = await linkResolver.ResolveLongUrlAsync(shortCode, cancellationToken);
    if (longUrl is null)
    {
        return Results.NotFound(new ErrorResponse("Short code not found."));
    }

    return Results.Redirect(longUrl, permanent: false);
});
```

