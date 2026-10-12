using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Exceptions;
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
        await SaveTranslatingConflictsAsync(shortUrl.Code, ct);
        return shortUrl;
    }

    public async Task UpdateAsync(ShortUrl shortUrl, CancellationToken ct = default)
    {
        _db.ShortUrls.Update(shortUrl);
        await SaveTranslatingConflictsAsync(shortUrl.Code, ct);
    }

    /// <summary>
    /// Saves, translating a unique-constraint violation on the code into a domain
    /// <see cref="DuplicateCodeException"/> so the API can return 409 even when two concurrent
    /// requests race past the existence check (the index is the real guarantee; this is the mapping).
    /// </summary>
    private async Task SaveTranslatingConflictsAsync(string? code, CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new DuplicateCodeException($"Code '{code}' is already in use.", ex);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) => ex.InnerException switch
    {
        SqliteException sqlite => sqlite.SqliteErrorCode == 19, // SQLITE_CONSTRAINT
        PostgresException pg => pg.SqlState == "23505",         // unique_violation
        _ => false
    };
}
