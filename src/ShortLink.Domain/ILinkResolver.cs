namespace ShortLink.Domain;

public interface ILinkResolver
{
    Task<string?> ResolveLongUrlAsync(string shortCode, CancellationToken cancellationToken = default);
}

