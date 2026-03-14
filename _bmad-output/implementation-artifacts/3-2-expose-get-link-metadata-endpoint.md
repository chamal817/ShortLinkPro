## Story 3.2: Expose GET /api/links/{shortCode} metadata endpoint

As a client (developer or dashboard),  
I want to retrieve the long URL and click count for a short code so that I can display analytics or verify the link.

### Acceptance Criteria

- **Given** a short code that exists in the system  
  **When** I request GET `/api/links/{shortCode}`  
  **Then** the server returns 200 with JSON containing `longUrl` and `clickCount` (and any other stored metadata)  
  **And** if the short code is unknown, the server returns 404 with the standard error format  
  **And** the endpoint is documented in OpenAPI/Swagger  
  **And** no authentication is required in MVP

### Implementation Plan

- **DTOs**
  - Define `GetLinkMetadataResponse` with:
    - `string ShortCode`
    - `string LongUrl`
    - `long ClickCount`
    - Optional: `DateTimeOffset CreatedAt`, any other metadata.
- **Repository**
  - Extend `ILinkRepository` with:
    - `Task<Link?> GetDetailsByShortCodeAsync(string shortCode, CancellationToken)`.
- **Endpoint**
  - Implement `GET /api/links/{shortCode}` minimal API:
    - Resolves link from repository (can use resolver or repo directly; repo is fine since this is not latency-critical like redirect).
    - If not found → 404 with `ErrorResponse`.
    - If found → 200 with `GetLinkMetadataResponse`.
- **OpenAPI**
  - Add summary/description for the endpoint.
  - Ensure response schemas are visible in Swagger.

### Sample Implementation Code (DTO and endpoint)

```csharp
public record GetLinkMetadataResponse(
    string ShortCode,
    string LongUrl,
    long ClickCount,
    DateTimeOffset CreatedAt);

app.MapGet("/api/links/{shortCode}", async (
    string shortCode,
    ILinkRepository linkRepository,
    CancellationToken cancellationToken) =>
{
    var link = await linkRepository.GetDetailsByShortCodeAsync(shortCode, cancellationToken);
    if (link is null)
    {
        return Results.NotFound(new ErrorResponse("Short code not found."));
    }

    var response = new GetLinkMetadataResponse(
        link.ShortCode,
        link.LongUrl,
        link.ClickCount,
        link.CreatedAt);

    return Results.Ok(response);
})
.WithName("GetLinkMetadata")
.WithOpenApi(operation =>
{
    operation.Summary = "Get metadata for a short link (long URL and click count).";
    operation.Responses["200"]!.Description = "Metadata for the requested short code.";
    operation.Responses["404"]!.Description = "Short code not found.";
    return operation;
});
```

