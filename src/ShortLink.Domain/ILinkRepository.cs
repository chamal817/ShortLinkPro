namespace ShortLink.Domain;

public interface ILinkRepository
{
    Task<bool> ExistsAsync(string shortCode, CancellationToken cancellationToken = default);
    Task<Link> AddAsync(Link link, CancellationToken cancellationToken = default);
    Task<Link?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);
    Task<Link?> GetDetailsByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);
    Task IncrementClickCountAsync(string shortCode, CancellationToken cancellationToken = default);
}
