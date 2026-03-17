using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShortLink.Domain;
using ShortLink.Infrastructure;
using Xunit;

namespace ShortLink.Api.UnitTests;

public class RedirectEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RedirectEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_WithExistingShortCode_UsesResolverAndReturnsRedirect()
    {
        const string shortCode = "redir01";
        const string longUrl = "https://redirect.example.com";

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("RedirectDb"));
            });
        });

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Links.Add(new Link
            {
                ShortCode = shortCode,
                LongUrl = longUrl,
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/" + shortCode);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(longUrl.TrimEnd('/'), response.Headers.Location?.ToString().TrimEnd('/'));
    }

    [Fact]
    public async Task Get_WithUnknownShortCode_Returns404()
    {
        using var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("RedirectDb_404"));
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/unknown-code");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

