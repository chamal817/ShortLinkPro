## Story 1.4: Expose POST /api/links create endpoint

As a client (developer or application),  
I want to POST a long URL and receive a short URL so that I can share or embed short links.

### Acceptance Criteria

- **Given** the API is running and the database is available  
  **When** I send POST `/api/links` with a valid JSON body containing the long URL  
  **Then** the system creates a new link and returns 201 with `shortCode` and `shortUrl` (full URL) in the response  
  **And** invalid or missing long URL returns 400 with error body per architecture  
  **And** the endpoint is documented in OpenAPI/Swagger  
  **And** (optional) the new mapping is written to Redis so the first redirect can be fast

### Implementation Plan

- **Request/response contracts**
  - Define `CreateLinkRequest` with property `string LongUrl`.
  - Define `CreateLinkResponse` with properties `string ShortCode`, `string ShortUrl`.
  - Define `ErrorResponse` for validation errors.
- **Endpoint**
  - Implement minimal API endpoint:
    - Route: `POST /api/links`.
    - Body: `CreateLinkRequest`.
    - Validates that:
      - `LongUrl` is not null/empty.
      - Max length 2048.
      - Absolute `http` or `https` URL.
  - On success:
    - Use `IShortCodeGenerator` and `ILinkRepository` to create a link.
    - Persist to database.
    - Return `201 Created` with `CreateLinkResponse`.
    - Set `Location` header to `/api/links/{shortCode}` or the redirect URL.
- **Base URL handling**
  - Read base URL from configuration (`ShortLink:BaseUrl`).
  - Compose `ShortUrl = $"{baseUrl}/{shortCode}"`.
- **Redis (optional)**
  - If Redis cache is enabled, write the mapping `<shortCode, longUrl>` after persistence.
- **OpenAPI**
  - Add summaries/descriptions for the endpoint and DTOs via `WithOpenApi`.

### Sample Implementation Code (Endpoint)

```csharp
public record CreateLinkRequest(string LongUrl);

public record CreateLinkResponse(string ShortCode, string ShortUrl);

public record ErrorResponse(string Error);

app.MapPost("/api/links", async (
    CreateLinkRequest request,
    IShortCodeGenerator shortCodeGenerator,
    ILinkRepository linkRepository,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.LongUrl))
    {
        return Results.BadRequest(new ErrorResponse("LongUrl is required."));
    }

    if (request.LongUrl.Length > 2048)
    {
        return Results.BadRequest(new ErrorResponse("LongUrl is too long."));
    }

    if (!Uri.TryCreate(request.LongUrl, UriKind.Absolute, out var uri) ||
        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
    {
        return Results.BadRequest(new ErrorResponse("LongUrl must be an absolute HTTP or HTTPS URL."));
    }

    var shortCode = await shortCodeGenerator.GenerateAsync(cancellationToken);

    var link = new Link
    {
        Id = Guid.NewGuid(),
        ShortCode = shortCode,
        LongUrl = request.LongUrl,
        CreatedAt = DateTimeOffset.UtcNow
    };

    await linkRepository.AddAsync(link, cancellationToken);

    var baseUrl = configuration["ShortLink:BaseUrl"] ?? "http://localhost:5000";
    var shortUrl = $"{baseUrl}/{shortCode}";

    var response = new CreateLinkResponse(shortCode, shortUrl);

    return Results.Created($"/api/links/{shortCode}", response);
})
.WithName("CreateLink")
.WithOpenApi();
```

