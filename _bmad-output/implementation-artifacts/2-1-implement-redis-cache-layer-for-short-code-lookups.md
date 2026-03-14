## Story 2.1: Implement Redis cache layer for short-code lookups

As a developer,  
I want the redirect path to use Redis for short-code → long-URL lookups so that redirect latency stays low and cache hit rate can exceed 80%.

### Acceptance Criteria

- **Given** Redis is configured and available  
  **When** the application needs to resolve a short code to a long URL  
  **Then** it checks Redis first (cache-aside)  
  **And** on cache miss it reads from PostgreSQL and populates Redis for subsequent requests  
  **And** cache key and value format follow the architecture (e.g. key = short_code, value = long_url)  
  **And** the API remains stateless (no in-memory state between requests)

### Implementation Plan

- **Abstractions**
  - Define `ILinkCache` in `ShortLink.Api` (or `ShortLink.Domain`) to represent cache operations:
    - `Task<string?> GetLongUrlAsync(string shortCode, CancellationToken)`.
    - `Task SetLongUrlAsync(string shortCode, string longUrl, CancellationToken)`.
- **Redis implementation**
  - Implement `RedisLinkCache` using `IDistributedCache` or `IConnectionMultiplexer`:
    - Use cache-aside pattern:
      - First check Redis.
      - On miss, load from repository and set cache.
  - Use key format like `link:{shortCode}` and value as the raw `longUrl`.
- **Resolver**
  - Introduce `ILinkResolver` that encapsulates:
    - Check cache via `ILinkCache`.
    - On miss, query `ILinkRepository` / `AppDbContext`.
    - Store result in Redis for next time.
- **Configuration**
  - Ensure `StackExchangeRedisCache` (or equivalent) is configured and injected.
  - Add optional TTL (e.g. 1–24 hours) for cache entries.

### Sample Implementation Code (ILinkCache)

```csharp
public interface ILinkCache
{
    Task<string?> GetLongUrlAsync(string shortCode, CancellationToken cancellationToken = default);
    Task SetLongUrlAsync(string shortCode, string longUrl, CancellationToken cancellationToken = default);
}
```

### Sample Implementation Code (RedisLinkCache)

```csharp
public class RedisLinkCache : ILinkCache
{
    private readonly IDistributedCache _cache;
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(6);

    public RedisLinkCache(IDistributedCache cache)
    {
        _cache = cache;
    }

    private static string BuildKey(string shortCode) => $"link:{shortCode}";

    public async Task<string?> GetLongUrlAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(shortCode);
        return await _cache.GetStringAsync(key, cancellationToken);
    }

    public async Task SetLongUrlAsync(string shortCode, string longUrl, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(shortCode);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DefaultTtl
        };

        await _cache.SetStringAsync(key, longUrl, options, cancellationToken);
    }
}
```

### Sample Implementation Code (ILinkResolver)

```csharp
public interface ILinkResolver
{
    Task<string?> ResolveLongUrlAsync(string shortCode, CancellationToken cancellationToken = default);
}

public class LinkResolver : ILinkResolver
{
    private readonly ILinkCache _cache;
    private readonly ILinkRepository _repository;

    public LinkResolver(ILinkCache cache, ILinkRepository repository)
    {
        _cache = cache;
        _repository = repository;
    }

    public async Task<string?> ResolveLongUrlAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetLongUrlAsync(shortCode, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
        {
            return cached;
        }

        var link = await _repository.GetByShortCodeAsync(shortCode, cancellationToken);
        if (link is null)
        {
            return null;
        }

        await _cache.SetLongUrlAsync(shortCode, link.LongUrl, cancellationToken);
        return link.LongUrl;
    }
}
```

