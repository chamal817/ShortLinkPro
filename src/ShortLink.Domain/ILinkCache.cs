namespace ShortLink.Domain;

public interface ILinkCache
{
    Task<string?> GetLongUrlAsync(string shortCode, CancellationToken cancellationToken = default);
    Task SetLongUrlAsync(string shortCode, string longUrl, CancellationToken cancellationToken = default);
}

