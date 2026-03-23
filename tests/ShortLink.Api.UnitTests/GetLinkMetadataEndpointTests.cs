using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShortLink.Domain;
using ShortLink.Infrastructure;
using Xunit;

namespace ShortLink.Api.UnitTests;

public class GetLinkMetadataEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GetLinkMetadataEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_WithExistingShortCode_ReturnsMetadata()
    {
        const string shortCode = "meta01";
        const string longUrl = "https://metadata.example.com";

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("MetadataDb"));
            });
        });

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Links.Add(new Link
            {
                ShortCode = shortCode,
                LongUrl = longUrl,
                CreatedAt = DateTime.UtcNow,
                ClickCount = 42
            });
            db.SaveChanges();
        }

        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/links/{shortCode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<GetLinkMetadataResponse>();
        Assert.NotNull(payload);
        Assert.Equal(shortCode, payload!.ShortCode);
        Assert.Equal(longUrl, payload.LongUrl);
        Assert.Equal(42, payload.ClickCount);
    }

    [Fact]
    public async Task Get_WithUnknownShortCode_Returns404WithError()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("MetadataDb_404"));
            });
        });

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/links/unknown-meta");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.False(string.IsNullOrWhiteSpace(error!.Error));
    }
}

