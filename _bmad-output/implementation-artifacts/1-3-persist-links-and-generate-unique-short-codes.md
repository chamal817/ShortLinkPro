## Story 1.3: Persist links and generate unique short codes

As a developer,  
I want the system to store link mappings and generate collision-free short codes so that each long URL gets a unique short link.

### Acceptance Criteria

- **Given** a long URL is accepted for shortening  
  **When** the system generates a short code and persists the mapping  
  **Then** the short code is unique (no collisions with existing codes)  
  **And** the mapping is stored in PostgreSQL (e.g. table with short_code, long_url, created_at)  
  **And** schema is versioned via migrations (EF Core or SQL scripts)  
  **And** the short code algorithm is documented (e.g. base62 + counter or random with collision check)

### Implementation Plan

- **Domain model**
  - Add `Link` entity in `ShortLink.Domain` with properties:
    - `Id` (GUID or long).
    - `ShortCode` (string).
    - `LongUrl` (string).
    - `CreatedAt` (DateTimeOffset).
- **Persistence**
  - Configure `Link` entity in `AppDbContext`:
    - Map to `links` table.
    - Unique index/constraint on `ShortCode`.
- **Short code generation**
  - Add `IShortCodeGenerator` interface with `Task<string> GenerateAsync(CancellationToken)`.
  - Implement `Base62ShortCodeGenerator` using:
    - A counter (e.g. from a sequence table) or random bytes.
    - Base62 encoding.
  - Ensure the generator checks for collisions and retries (bounded by a small max retries).
- **Repository**
  - Add `ILinkRepository` with methods to:
    - Check if a short code exists.
    - Add a new link.
    - Get link by short code.
- **Migrations**
  - Add initial EF Core migration for `links` table and apply it.

### Sample Implementation Code (Link entity)

```csharp
namespace ShortLink.Domain.Links;

public class Link
{
    public Guid Id { get; set; }
    public string ShortCode { get; set; } = default!;
    public string LongUrl { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
}
```

### Sample Implementation Code (DbContext configuration)

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<Link>(entity =>
    {
        entity.ToTable("links");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => x.ShortCode).IsUnique();
        entity.Property(x => x.ShortCode).IsRequired().HasMaxLength(16);
        entity.Property(x => x.LongUrl).IsRequired().HasMaxLength(2048);
        entity.Property(x => x.CreatedAt).IsRequired();
    });
}
```

