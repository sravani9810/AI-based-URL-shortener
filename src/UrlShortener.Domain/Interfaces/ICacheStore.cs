namespace UrlShortener.Domain.Interfaces;

/// <summary>
/// Minimal key/value cache contract used for the redirect read path (cache-aside, ADR-0003).
/// Implementations must degrade gracefully: a cache failure returns a miss / no-op, never throws,
/// so the redirect falls back to the database instead of failing.
/// </summary>
public interface ICacheStore
{
    /// <summary>Returns the cached value for a key, or null on miss or cache failure.</summary>
    Task<string?> GetAsync(string key, CancellationToken ct = default);

    /// <summary>Stores a value with an optional TTL. No-op on cache failure.</summary>
    Task SetAsync(string key, string value, TimeSpan? ttl = null, CancellationToken ct = default);

    /// <summary>Removes a key. No-op on cache failure.</summary>
    Task RemoveAsync(string key, CancellationToken ct = default);
}
