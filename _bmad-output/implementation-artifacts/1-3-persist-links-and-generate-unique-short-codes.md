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

### Tasks / Subtasks

- [x] Domain: Add `IShortCodeGenerator` and `ILinkRepository` interfaces.
- [x] Infrastructure: Implement `Base62ShortCodeGenerator` (Base62, 8 chars, random bytes); implement `LinkRepository` (ExistsAsync, AddAsync, GetByShortCodeAsync).
- [x] Persistence: Configure `Link` in `AppDbContext` with table `links`, unique index on ShortCode, CreatedAt required; add migration to rename table to `links`.
- [x] DI: Register `IShortCodeGenerator` → `Base62ShortCodeGenerator`, `ILinkRepository` → `LinkRepository` in Program.cs.
- [x] Documentation: Update `docs/short-code-algorithm.md` with interface and implementation names.
- [x] Tests: Unit tests for `Base62ShortCodeGenerator` (length 8, base62 chars only, cancellation, distinctness); unit tests for `LinkRepository` (ExistsAsync, AddAsync, GetByShortCodeAsync) with InMemory DB.

### Dev Agent Record

**Implemented:** Domain: `IShortCodeGenerator` (`Task<string> GenerateAsync(CancellationToken)`), `ILinkRepository` (ExistsAsync, AddAsync, GetByShortCodeAsync). Infrastructure: `Base62ShortCodeGenerator` (Base62 charset, 8 chars, random bytes; collision handling left to caller via retry on unique constraint), `LinkRepository` using AppDbContext. AppDbContext: Link mapped to table `links`, unique index on ShortCode, CreatedAt required, ShortCode/LongUrl max lengths. Migration `RenameLinksTable` renames table `Links` → `links`. Program.cs: Scoped registration for IShortCodeGenerator and ILinkRepository. docs/short-code-algorithm.md updated to reference ShortLink.Domain and ShortLink.Infrastructure types.

**Tests:** `Base62ShortCodeGeneratorTests`: GenerateAsync returns length 8, only base62 chars, respects cancellation, multiple calls produce different codes. `LinkRepositoryTests`: ExistsAsync false when missing/true after add; AddAsync persists and sets Id; GetByShortCodeAsync null when missing/returns link when exists. All 12 tests pass (3 health + 4 generator + 5 repository).

### File List

- `src/ShortLink.Domain/IShortCodeGenerator.cs` (new)
- `src/ShortLink.Domain/ILinkRepository.cs` (new)
- `src/ShortLink.Infrastructure/Base62ShortCodeGenerator.cs` (new)
- `src/ShortLink.Infrastructure/LinkRepository.cs` (new)
- `src/ShortLink.Infrastructure/AppDbContext.cs` (modified: ToTable("links"), CreatedAt, IsRequired)
- `src/ShortLink.Infrastructure/Migrations/20260314171139_RenameLinksTable.cs` (new)
- `src/ShortLink.Infrastructure/Migrations/20260314171139_RenameLinksTable.Designer.cs` (new)
- `src/ShortLink.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` (modified)
- `src/ShortLink.Api/Program.cs` (modified: DI for IShortCodeGenerator, ILinkRepository)
- `docs/short-code-algorithm.md` (modified: implementation section)
- `tests/ShortLink.Api.UnitTests/Base62ShortCodeGeneratorTests.cs` (new)
- `tests/ShortLink.Api.UnitTests/LinkRepositoryTests.cs` (new)

