using UrlShortener.Domain.Entities;

namespace UrlShortener.Domain.Interfaces;

/// <summary>Persistence contract for <see cref="ShortUrl"/> aggregates.</summary>
public interface IShortUrlRepository
{
    /// <summary>Returns the mapping for a code, or null if none exists.</summary>
    Task<ShortUrl?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>True if a mapping already uses this code (used for custom-alias conflict checks).</summary>
    Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);

    /// <summary>Inserts a new mapping and returns it with its generated <see cref="ShortUrl.Id"/>.</summary>
    Task<ShortUrl> AddAsync(ShortUrl shortUrl, CancellationToken ct = default);

    /// <summary>Persists changes to an already-tracked mapping (e.g. assigning the code after insert).</summary>
    Task UpdateAsync(ShortUrl shortUrl, CancellationToken ct = default);
}
