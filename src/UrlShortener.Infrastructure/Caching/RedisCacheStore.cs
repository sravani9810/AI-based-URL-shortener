using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using UrlShortener.Domain.Interfaces;

namespace UrlShortener.Infrastructure.Caching;

/// <summary>
/// Redis-backed <see cref="ICacheStore"/>. Every operation is wrapped so a Redis outage degrades to a
/// cache miss / no-op rather than failing the redirect (resiliency, ADR-0002/0003).
/// </summary>
public sealed class RedisCacheStore : ICacheStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheStore> _logger;

    public RedisCacheStore(IConnectionMultiplexer redis, ILogger<RedisCacheStore> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var value = await _redis.GetDatabase().StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for {Key}; treating as cache miss.", key);
            return null;
        }
    }

    public async Task SetAsync(string key, string value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        try
        {
            await _redis.GetDatabase().StringSetAsync(key, value, ttl, When.Always);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for {Key}; continuing without caching.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _redis.GetDatabase().KeyDeleteAsync(key);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis DELETE failed for {Key}.", key);
        }
    }
}
