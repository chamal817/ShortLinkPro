using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShortLink.Domain;
using ShortLink.Infrastructure;
using Xunit;

namespace ShortLink.Api.UnitTests;

public class LinkResolverTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IDistributedCache _cache;
    private readonly ILinkCache _linkCache;
    private readonly ILinkRepository _repository;
    private readonly ILinkResolver _resolver;

    public LinkResolverTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("LinkResolver_" + Guid.NewGuid().ToString("N"))
            .Options;

        _db = new AppDbContext(options);
        _repository = new LinkRepository(_db);

        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var provider = services.BuildServiceProvider();
        _cache = provider.GetRequiredService<IDistributedCache>();

        _linkCache = new RedisLinkCache(_cache);
        _resolver = new LinkResolver(_linkCache, _repository);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task ResolveLongUrlAsync_ReturnsFromCache_WhenPresent()
    {
        const string shortCode = "cached1";
        const string longUrl = "https://cached.example.com";

        await _linkCache.SetLongUrlAsync(shortCode, longUrl);

        var result = await _resolver.ResolveLongUrlAsync(shortCode);

        Assert.Equal(longUrl, result);
    }

    [Fact]
    public async Task ResolveLongUrlAsync_LoadsFromRepositoryAndCaches_WhenMissingFromCache()
    {
        const string shortCode = "miss1";
        const string longUrl = "https://miss.example.com";

        _db.Links.Add(new Link
        {
            ShortCode = shortCode,
            LongUrl = longUrl,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _resolver.ResolveLongUrlAsync(shortCode);

        Assert.Equal(longUrl, result);

        var cached = await _linkCache.GetLongUrlAsync(shortCode);
        Assert.Equal(longUrl, cached);
    }

    [Fact]
    public async Task ResolveLongUrlAsync_ReturnsNull_WhenNotFoundAnywhere()
    {
        var result = await _resolver.ResolveLongUrlAsync("unknown");
        Assert.Null(result);
    }
}

