using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<ShortLink> ShortLinks => Set<ShortLink>();
    public DbSet<ClickEvent> ClickEvents => Set<ClickEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<ShortLink>(e =>
        {
            e.HasIndex(s => s.ShortCode).IsUnique();
        });
        builder.Entity<ClickEvent>(e =>
        {
            e.HasOne(c => c.ShortLink)
             .WithMany()
             .HasForeignKey(c => c.ShortLinkId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(c => c.ShortLinkId);
            e.HasIndex(c => c.ClickedAt);
        });
    }
}