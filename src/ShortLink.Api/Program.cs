using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Npgsql;
using ShortLink.Domain;
using ShortLink.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IShortCodeGenerator, Base62ShortCodeGenerator>();
builder.Services.AddScoped<ILinkRepository, LinkRepository>();
builder.Services.AddScoped<ILinkCache, RedisLinkCache>();
builder.Services.AddScoped<ILinkResolver, LinkResolver>();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (PostgresException ex) when (ex.SqlState == "28P01" || (ex.SqlState?.Length == 5 && ex.SqlState.StartsWith("08")))
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Database connection failed (SqlState: {SqlState}). Returning 503.", ex.SqlState);
        context.Response.StatusCode = 503;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new ErrorResponse("Service temporarily unavailable. Database connection failed. Check ConnectionStrings:Default or run 'docker compose up -d'."));
    }
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
    {
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(ex, "Database migration skipped: cannot connect to PostgreSQL. Start Docker (docker compose up -d) or check ConnectionStrings:Default. /health/db will return 503 until the database is available.");
        }
    }
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/health/db", async (AppDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return canConnect ? Results.Ok(new { status = "ok", database = "connected" }) : Results.StatusCode(503);
});

app.MapPost("/api/links", async (
    CreateLinkRequest request,
    IShortCodeGenerator shortCodeGenerator,
    ILinkRepository linkRepository,
    ILinkCache linkCache,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.LongUrl))
        return Results.BadRequest(new ErrorResponse("LongUrl is required."));

    if (request.LongUrl.Length > 2048)
        return Results.BadRequest(new ErrorResponse("LongUrl is too long."));

    if (!Uri.TryCreate(request.LongUrl, UriKind.Absolute, out var uri) ||
        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        return Results.BadRequest(new ErrorResponse("LongUrl must be an absolute HTTP or HTTPS URL."));

    const int maxRetries = 5;
    string? shortCode = null;
    for (var i = 0; i < maxRetries; i++)
    {
        var candidate = await shortCodeGenerator.GenerateAsync(cancellationToken);
        if (!await linkRepository.ExistsAsync(candidate, cancellationToken))
        {
            shortCode = candidate;
            break;
        }
    }
    if (shortCode == null)
        return Results.StatusCode(503);

    var link = new Link
    {
        ShortCode = shortCode,
        LongUrl = request.LongUrl,
        CreatedAt = DateTime.UtcNow
    };
    await linkRepository.AddAsync(link, cancellationToken);

    try
    {
        await linkCache.SetLongUrlAsync(shortCode, request.LongUrl, cancellationToken);
    }
    catch
    {
    }

    var baseUrl = configuration["ShortLink:BaseUrl"] ?? "http://localhost:5000";
    var shortUrl = $"{baseUrl.TrimEnd('/')}/{shortCode}";
    var response = new CreateLinkResponse(shortCode, shortUrl);
    return Results.Created($"/api/links/{shortCode}", response);
})
.WithName("CreateLink");

app.MapGet("/{shortCode}", async (string shortCode, ILinkResolver resolver, CancellationToken cancellationToken) =>
{
    var longUrl = await resolver.ResolveLongUrlAsync(shortCode, cancellationToken);
    return longUrl is null
        ? Results.NotFound()
        : Results.Redirect(longUrl);
})
.WithName("ResolveLink");

app.Run();

// Request/response contracts for POST /api/links
public record CreateLinkRequest(string LongUrl);
public record CreateLinkResponse(string ShortCode, string ShortUrl);
public record ErrorResponse(string Error);

// Expose for WebApplicationFactory in tests
public partial class Program { }
