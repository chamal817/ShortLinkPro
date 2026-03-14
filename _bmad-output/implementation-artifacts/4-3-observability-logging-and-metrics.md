## Story 4.3: Observability (logging and metrics)

As an operator,  
I want structured logs and metrics so that I can validate SLAs and troubleshoot issues.

### Acceptance Criteria

- **Given** the API is running and handling requests  
  **Then** logs are structured (e.g. JSON or key-value) and include relevant context (e.g. short code, status, duration)  
  **And** metrics (or equivalent) expose redirect count, redirect latency (e.g. P95), and cache hit rate (or cache hit/miss counts)  
  **And** observability is documented so that operators can use it to validate NFR-O2 and NFR-P1/P3

### Implementation Plan

- **Structured logging**
  - Use ASP.NET Core built-in logging or a provider like Serilog:
    - Configure JSON output (for log aggregation).
    - Include correlation IDs and request context.
  - Log key events:
    - Link creation (shortCode, status).
    - Redirects (shortCode, status, duration, cache hit/miss).
    - Errors and timeouts.
- **Metrics**
  - Use a metrics library (e.g. OpenTelemetry Metrics, Prometheus-net) or ASP.NET Core built-in metrics:
    - Counter: total redirects.
    - Histogram: redirect latency.
    - Counters: cache hits and misses.
  - Expose metrics endpoint (e.g. `/metrics`) if using Prometheus.
- **Instrumentation**
  - Wrap redirect and cache resolver logic to:
    - Record start/stop times.
    - Increment appropriate counters and histograms.
- **Documentation**
  - Describe:
    - Where logs go (console, file).
    - How to scrape metrics and which ones matter for SLAs.

### Sample Implementation Code (Serilog JSON logging)

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
```

### Sample Implementation Code (Prometheus metrics for redirect)

```csharp
using Prometheus;

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

app.UseHttpMetrics();
app.MapMetrics(); // exposes /metrics

app.MapGet("/{shortCode}", async (
    string shortCode,
    ILinkResolver linkResolver,
    CancellationToken cancellationToken) =>
{
    var stopwatch = Stopwatch.StartNew();
    var cacheHit = "false";

    var result = await linkResolver.ResolveWithCacheInfoAsync(shortCode, cancellationToken);
    string status;

    if (result.LongUrl is null)
    {
        status = "404";
        redirectCounter.WithLabels(status, cacheHit).Inc();
        redirectDuration.Observe(stopwatch.Elapsed.TotalSeconds);
        return Results.NotFound(new ErrorResponse("Short code not found."));
    }

    cacheHit = result.CacheHit ? "true" : "false";
    status = "302";

    redirectCounter.WithLabels(status, cacheHit).Inc();
    redirectDuration.Observe(stopwatch.Elapsed.TotalSeconds);

    return Results.Redirect(result.LongUrl, permanent: false);
});
```

