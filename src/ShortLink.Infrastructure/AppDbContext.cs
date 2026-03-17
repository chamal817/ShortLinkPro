using Microsoft.EntityFrameworkCore;
using ShortLink.Domain;

namespace ShortLink.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Link> Links => Set<Link>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Link>(e =>
        {
            e.ToTable("links");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ShortCode).IsUnique();
            e.Property(x => x.ShortCode).IsRequired().HasMaxLength(32);
            e.Property(x => x.LongUrl).IsRequired().HasMaxLength(2048);
            e.Property(x => x.CreatedAt).IsRequired();
        });
    }
}
