using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShortLink.Infrastructure;
using Xunit;

namespace ShortLink.Api.UnitTests;

public class CreateLinkEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CreateLinkEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("CreateLinkTestDb"));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Post_ValidLongUrl_Returns201WithShortCodeAndShortUrl()
    {
        var request = new CreateLinkRequest("https://example.com/valid-path");
        var response = await _client.PostAsJsonAsync("/api/links", request);
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateLinkResponse>();
        Assert.NotNull(body);
        Assert.NotNull(body.ShortCode);
        Assert.NotEmpty(body.ShortCode);
        Assert.EndsWith(body.ShortCode, body.ShortUrl);
    }

    [Fact]
    public async Task Post_MissingLongUrl_Returns400()
    {
        var request = new CreateLinkRequest("");
        var response = await _client.PostAsJsonAsync("/api/links", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.NotNull(body.Error);
    }

    [Fact]
    public async Task Post_EmptyLongUrl_Returns400()
    {
        var request = new CreateLinkRequest("   ");
        var response = await _client.PostAsJsonAsync("/api/links", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidScheme_Returns400()
    {
        var request = new CreateLinkRequest("ftp://example.com/file");
        var response = await _client.PostAsJsonAsync("/api/links", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Contains("HTTP or HTTPS", body.Error);
    }

    [Fact]
    public async Task Post_LongUrlExceeds2048_Returns400()
    {
        var request = new CreateLinkRequest("https://example.com/" + new string('a', 2040));
        var response = await _client.PostAsJsonAsync("/api/links", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidFormat_Returns400()
    {
        var request = new CreateLinkRequest("not-a-url");
        var response = await _client.PostAsJsonAsync("/api/links", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidUrl_ReturnsLocationHeader()
    {
        var request = new CreateLinkRequest("https://example.com/another");
        var response = await _client.PostAsJsonAsync("/api/links", request);
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.TryGetValues("Location", out var locations));
        var location = locations.Single();
        Assert.StartsWith("/api/links/", location);
    }

    private sealed record CreateLinkRequest(string LongUrl);
    private sealed record CreateLinkResponse(string ShortCode, string ShortUrl);
    private sealed record ErrorResponse(string Error);
}
