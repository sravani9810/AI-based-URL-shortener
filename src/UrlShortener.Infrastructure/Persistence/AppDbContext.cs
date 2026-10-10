using Microsoft.EntityFrameworkCore;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence;

/// <summary>EF Core context. SQLite in the prototype; provider-swappable (ADR-0003).</summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ShortUrl> ShortUrls => Set<ShortUrl>();
    public DbSet<ClickEvent> ClickEvents => Set<ClickEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ShortUrl>();
        entity.HasKey(e => e.Id);
        entity.Property(e => e.TargetUrl).IsRequired().HasMaxLength(2048);
        entity.Property(e => e.Code).HasMaxLength(64);

        // Unique on Code, but filtered so the transient null between insert and code
        // assignment (auto-generated links) does not collide across concurrent inserts.
        entity.HasIndex(e => e.Code)
              .IsUnique()
              .HasFilter("\"Code\" IS NOT NULL");

        var click = modelBuilder.Entity<ClickEvent>();
        click.HasKey(c => c.Id);
        click.Property(c => c.Code).IsRequired().HasMaxLength(64);
        click.Property(c => c.Referrer).HasMaxLength(2048);
        click.Property(c => c.UserAgent).HasMaxLength(512);
        click.HasIndex(c => c.Code); // analytics queries filter by code
    }
}
