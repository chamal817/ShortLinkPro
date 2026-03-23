using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.NpgSql;
using HealthChecks.Redis;
using Prometheus;
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

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddNpgSql(
        builder.Configuration.GetConnectionString("Default")!,
        name: "postgres",
        tags: new[] { "ready" })
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis")!,
        name: "redis",
        tags: new[] { "ready" });

var redirectCounter = Metrics.CreateCounter(
    "shortlink_redirect_total",
    "Total number of redirects",
    new CounterConfiguration
    {
        LabelNames = new[] { "status", "cache_hit" }
    });

var redirectDuration = Metrics.CreateHistogram(
    "shortlink_redirect_duration_seconds",
    "Redirect duration in seconds",
    new HistogramConfiguration
    {
        Buckets = Histogram.LinearBuckets(start: 0.01, width: 0.01, count: 20)
    });

var cacheHitCounter = Metrics.CreateCounter(
    "shortlink_cache_hit_total",
    "Total number of cache hits",
    new CounterConfiguration
    {
        LabelNames = new[] { "operation" }
    });

var cacheMissCounter = Metrics.CreateCounter(
    "shortlink_cache_miss_total",
    "Total number of cache misses",
    new CounterConfiguration
    {
        LabelNames = new[] { "operation" }
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpMetrics();

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

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.MapMetrics();

app.MapPost("/api/links", async (
    CreateLinkRequest request,
    IShortCodeGenerator shortCodeGenerator,
    ILinkRepository linkRepository,
    ILinkCache linkCache,
    IConfiguration configuration,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var start = Stopwatch.GetTimestamp();

    logger.LogInformation("CreateLink request received for URL {LongUrl}", request.LongUrl);

    if (string.IsNullOrWhiteSpace(request.LongUrl))
    {
        logger.LogWarning("CreateLink validation failed: LongUrl is required");
        return Results.BadRequest(new ErrorResponse("LongUrl is required."));
    }

    if (request.LongUrl.Length > 2048)
    {
        logger.LogWarning("CreateLink validation failed: LongUrl too long (length {Length})", request.LongUrl.Length);
        return Results.BadRequest(new ErrorResponse("LongUrl is too long."));
    }

    if (!Uri.TryCreate(request.LongUrl, UriKind.Absolute, out var uri) ||
        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
    {
        logger.LogWarning("CreateLink validation failed: LongUrl invalid format {LongUrl}", request.LongUrl);
        return Results.BadRequest(new ErrorResponse("LongUrl must be an absolute HTTP or HTTPS URL."));
    }

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
    {
        logger.LogError("CreateLink failed: could not generate unique short code after {MaxRetries} retries", maxRetries);
        return Results.StatusCode(503);
    }

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
        cacheHitCounter.WithLabels("set").Inc();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "CreateLink: failed to set cache for short code {ShortCode}", shortCode);
        cacheMissCounter.WithLabels("set_failed").Inc();
    }

    var baseUrl = configuration["ShortLink:BaseUrl"] ?? "http://localhost:5000";
    var shortUrl = $"{baseUrl.TrimEnd('/')}/{shortCode}";
    var response = new CreateLinkResponse(shortCode, shortUrl);

    var elapsedSeconds = (Stopwatch.GetTimestamp() - start) / (double)Stopwatch.Frequency;
    logger.LogInformation("CreateLink succeeded for short code {ShortCode} in {ElapsedSeconds:F4}s", shortCode, elapsedSeconds);

    return Results.Created($"/api/links/{shortCode}", response);
})
.WithName("CreateLink");

app.MapGet("/api/links/{shortCode}", async (
    string shortCode,
    ILinkRepository linkRepository,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    logger.LogInformation("GetLinkMetadata requested for short code {ShortCode}", shortCode);

    var link = await linkRepository.GetDetailsByShortCodeAsync(shortCode, cancellationToken);
    if (link is null)
    {
        logger.LogWarning("GetLinkMetadata: short code {ShortCode} not found", shortCode);
        return Results.NotFound(new ErrorResponse("Short code not found."));
    }

    var response = new GetLinkMetadataResponse(
        link.ShortCode,
        link.LongUrl,
        link.ClickCount,
        link.CreatedAt);

    logger.LogInformation("GetLinkMetadata: returning metadata for short code {ShortCode}", shortCode);

    return Results.Ok(response);
})
.WithName("GetLinkMetadata");

app.MapGet("/{shortCode}", async (string shortCode, ILinkResolver resolver, ILinkRepository repository, ILogger<Program> logger, CancellationToken cancellationToken) =>
{
    var stopwatch = Stopwatch.StartNew();
    var cacheHitLabel = "false";

    logger.LogInformation("Redirect requested for short code {ShortCode}", shortCode);

    var longUrl = await resolver.ResolveLongUrlAsync(shortCode, cancellationToken);
    if (longUrl is null)
    {
        redirectCounter.WithLabels("404", cacheHitLabel).Inc();
        redirectDuration.Observe(stopwatch.Elapsed.TotalSeconds);

        logger.LogWarning("Redirect failed: short code {ShortCode} not found", shortCode);
        return Results.NotFound();
    }

    // We don't currently expose cache-hit info from resolver; approximate using cache counter metrics.
    redirectCounter.WithLabels("302", cacheHitLabel).Inc();
    redirectDuration.Observe(stopwatch.Elapsed.TotalSeconds);

    try
    {
        await repository.IncrementClickCountAsync(shortCode, cancellationToken);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to increment click count for short code {ShortCode}", shortCode);
    }

    logger.LogInformation("Redirecting short code {ShortCode} to {LongUrl} in {ElapsedSeconds:F4}s", shortCode, longUrl, stopwatch.Elapsed.TotalSeconds);

    return Results.Redirect(longUrl);
})
.WithName("ResolveLink");

app.Run();

// Request/response contracts for POST /api/links
public record CreateLinkRequest(string LongUrl);
public record CreateLinkResponse(string ShortCode, string ShortUrl);
public record ErrorResponse(string Error);
public record GetLinkMetadataResponse(string ShortCode, string LongUrl, long ClickCount, DateTime CreatedAt);

// Expose for WebApplicationFactory in tests
public partial class Program { }
