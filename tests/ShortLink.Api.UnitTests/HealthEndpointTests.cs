using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShortLink.Infrastructure;
using Xunit;

namespace ShortLink.Api.UnitTests;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("TestDb"));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Get_Health_Returns200Ok()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_Health_ReturnsJsonWithStatusOk()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("ok", body.Status);
    }

    [Fact]
    public async Task Get_HealthDb_Returns200WhenDatabaseConnected()
    {
        var response = await _client.GetAsync("/health/db");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<HealthDbResponse>();
        Assert.NotNull(body);
        Assert.Equal("ok", body.Status);
        Assert.Equal("connected", body.Database);
    }

    private sealed record HealthResponse(string Status);
    private sealed record HealthDbResponse(string Status, string Database);
}
