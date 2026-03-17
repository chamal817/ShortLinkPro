using ShortLink.Domain;

namespace ShortLink.Infrastructure;

public sealed class LinkResolver : ILinkResolver
{
    private readonly ILinkCache _cache;
    private readonly ILinkRepository _repository;

    public LinkResolver(ILinkCache cache, ILinkRepository repository)
    {
        _cache = cache;
        _repository = repository;
    }

    public async Task<string?> ResolveLongUrlAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetLongUrlAsync(shortCode, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
        {
            return cached;
        }

        var link = await _repository.GetByShortCodeAsync(shortCode, cancellationToken);
        if (link is null)
        {
            return null;
        }

        await _cache.SetLongUrlAsync(shortCode, link.LongUrl, cancellationToken);
        return link.LongUrl;
    }
}

