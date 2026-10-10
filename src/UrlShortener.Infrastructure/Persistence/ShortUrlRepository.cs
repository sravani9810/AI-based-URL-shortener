using Microsoft.EntityFrameworkCore;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Interfaces;

namespace UrlShortener.Infrastructure.Persistence;

/// <summary>EF Core-backed <see cref="IShortUrlRepository"/>.</summary>
public class ShortUrlRepository : IShortUrlRepository
{
    private readonly AppDbContext _db;

    public ShortUrlRepository(AppDbContext db) => _db = db;

    public Task<ShortUrl?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        _db.ShortUrls.AsNoTracking().FirstOrDefaultAsync(u => u.Code == code, ct);

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct = default) =>
        _db.ShortUrls.AnyAsync(u => u.Code == code, ct);

    public async Task<ShortUrl> AddAsync(ShortUrl shortUrl, CancellationToken ct = default)
    {
        _db.ShortUrls.Add(shortUrl);
        await _db.SaveChangesAsync(ct);
        return shortUrl;
    }

    public async Task UpdateAsync(ShortUrl shortUrl, CancellationToken ct = default)
    {
        _db.ShortUrls.Update(shortUrl);
        await _db.SaveChangesAsync(ct);
    }
}
