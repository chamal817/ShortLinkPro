using Microsoft.EntityFrameworkCore;
using ShortLink.Domain;

namespace ShortLink.Infrastructure;

public sealed class LinkRepository : ILinkRepository
{
    private readonly AppDbContext _db;

    public LinkRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> ExistsAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        return await _db.Links.AnyAsync(l => l.ShortCode == shortCode, cancellationToken);
    }

    public async Task<Link> AddAsync(Link link, CancellationToken cancellationToken = default)
    {
        _db.Links.Add(link);
        await _db.SaveChangesAsync(cancellationToken);
        return link;
    }

    public async Task<Link?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        return await _db.Links.AsNoTracking().FirstOrDefaultAsync(l => l.ShortCode == shortCode, cancellationToken);
    }
}
