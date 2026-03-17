using Microsoft.Extensions.Caching.Distributed;
using ShortLink.Domain;

namespace ShortLink.Infrastructure;

public sealed class RedisLinkCache : ILinkCache
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

