using Microsoft.EntityFrameworkCore;
using ShortLink.Domain;
using ShortLink.Infrastructure;
using Xunit;

namespace ShortLink.Api.UnitTests;

public class LinkRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly LinkRepository _repo;

    public LinkRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "LinkRepo_" + Guid.NewGuid().ToString("N"))
            .Options;
        _db = new AppDbContext(options);
        _repo = new LinkRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenShortCodeDoesNotExist()
    {
        var result = await _repo.ExistsAsync("nonexistent");
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_AfterAdd()
    {
        var link = new Link { ShortCode = "abc12345", LongUrl = "https://example.com", CreatedAt = DateTime.UtcNow };
        await _repo.AddAsync(link);
        var result = await _repo.ExistsAsync("abc12345");
        Assert.True(result);
    }

    [Fact]
    public async Task AddAsync_PersistsLink_AndSetsId()
    {
        var link = new Link { ShortCode = "xyz98765", LongUrl = "https://foo.com", CreatedAt = DateTime.UtcNow };
        var added = await _repo.AddAsync(link);
        Assert.True(added.Id != 0);
        var found = await _repo.GetByShortCodeAsync("xyz98765");
        Assert.NotNull(found);
        Assert.Equal(added.Id, found.Id);
        Assert.Equal("xyz98765", found.ShortCode);
        Assert.Equal("https://foo.com", found.LongUrl);
    }

    [Fact]
    public async Task GetByShortCodeAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _repo.GetByShortCodeAsync("missing");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByShortCodeAsync_ReturnsLink_WhenExists()
    {
        var link = new Link { ShortCode = "getme", LongUrl = "https://get.me", CreatedAt = DateTime.UtcNow };
        await _repo.AddAsync(link);
        var result = await _repo.GetByShortCodeAsync("getme");
        Assert.NotNull(result);
        Assert.Equal("getme", result.ShortCode);
        Assert.Equal("https://get.me", result.LongUrl);
    }
}
