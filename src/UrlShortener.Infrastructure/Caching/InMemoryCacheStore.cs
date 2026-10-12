using System.Collections.Concurrent;
using UrlShortener.Domain.Interfaces;

namespace UrlShortener.Infrastructure.Caching;

/// <summary>
/// Thread-safe in-process <see cref="ICacheStore"/> used when Redis is not configured, and in tests.
/// Entries carry an absolute expiry and are evicted lazily on read.
/// </summary>
public sealed class InMemoryCacheStore : ICacheStore
{
    private readonly ConcurrentDictionary<string, Entry> _store = new();

    private readonly record struct Entry(string Value, DateTimeOffset? ExpiresAt);

    public Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        if (_store.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt is { } e && e <= DateTimeOffset.UtcNow)
            {
                _store.TryRemove(key, out _);
                return Task.FromResult<string?>(null);
            }
            return Task.FromResult<string?>(entry.Value);
        }
        return Task.FromResult<string?>(null);
    }

    public Task SetAsync(string key, string value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var expiresAt = ttl is { } t ? DateTimeOffset.UtcNow.Add(t) : (DateTimeOffset?)null;
        _store[key] = new Entry(value, expiresAt);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
