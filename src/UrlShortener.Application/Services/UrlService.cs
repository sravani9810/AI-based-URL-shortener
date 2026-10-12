using System.Text.Json;
using System.Text.RegularExpressions;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Exceptions;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Interfaces;

namespace UrlShortener.Application.Services;

/// <summary>
/// Creates and resolves short links. Enforces URL/alias/expiry validation, assigns Base62 codes from
/// the row id for auto links (ADR-0007), resolves through a cache-aside read path, and reports expiry.
/// </summary>
public sealed partial class UrlService : IUrlService
{
    private const int MaxUrlLength = 2048;
    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromHours(1);

    [GeneratedRegex("^[A-Za-z0-9_-]{3,64}$")]
    private static partial Regex AliasPattern();

    private static readonly HashSet<string> ReservedCodes =
        new(StringComparer.OrdinalIgnoreCase) { "api", "healthz", "swagger", "favicon.ico", "index.html" };

    private readonly IShortUrlRepository _repository;
    private readonly ICodeGenerator _codeGenerator;
    private readonly ICacheStore _cache;
    private readonly IClickAnalyticsRepository _analytics;
    private readonly TimeProvider _timeProvider;

    public UrlService(
        IShortUrlRepository repository,
        ICodeGenerator codeGenerator,
        ICacheStore cache,
        IClickAnalyticsRepository analytics,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _codeGenerator = codeGenerator;
        _cache = cache;
        _analytics = analytics;
        _timeProvider = timeProvider;
    }

    public async Task<ShortUrlDto> CreateAsync(CreateShortUrlRequest request, CancellationToken ct = default)
    {
        var targetUrl = NormalizeAndValidateUrl(request.Url);
        var now = _timeProvider.GetUtcNow();
        ValidateExpiry(request.ExpiresAt, now);

        ShortUrl entity;
        if (!string.IsNullOrWhiteSpace(request.Alias))
        {
            var alias = request.Alias.Trim();
            ValidateAlias(alias);
            if (await _repository.CodeExistsAsync(alias, ct))
                throw new ConflictException($"Alias '{alias}' is already in use.");

            entity = await _repository.AddAsync(new ShortUrl
            {
                Code = alias,
                TargetUrl = targetUrl,
                IsCustomAlias = true,
                CreatedAt = now,
                ExpiresAt = request.ExpiresAt
            }, ct);
        }
        else
        {
            // Auto code: insert first to obtain the monotonic id, then derive the Base62 code.
            entity = await _repository.AddAsync(new ShortUrl
            {
                Code = null,
                TargetUrl = targetUrl,
                IsCustomAlias = false,
                CreatedAt = now,
                ExpiresAt = request.ExpiresAt
            }, ct);

            entity.Code = _codeGenerator.Encode(entity.Id);
            await _repository.UpdateAsync(entity, ct);
        }

        var dto = ToDto(entity);
        await CacheAsync(dto, now, ct); // write-through so the first redirect is a cache hit
        return dto;
    }

    public async Task<ResolveResult> ResolveAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return ResolveResult.NotFound;

        code = code.Trim();
        var now = _timeProvider.GetUtcNow();
        var key = CacheKey(code);

        // Cache-aside read: try cache, then fall back to the database and populate the cache.
        ShortUrlDto? dto = null;
        var cached = await _cache.GetAsync(key, ct);
        if (cached is not null)
            dto = TryDeserialize(cached);

        if (dto is null)
        {
            var entity = await _repository.GetByCodeAsync(code, ct);
            if (entity is null)
                return ResolveResult.NotFound;

            dto = ToDto(entity);
            await CacheAsync(dto, now, ct);
        }

        if (dto.ExpiresAt is { } expiry && expiry <= now)
        {
            await _cache.RemoveAsync(key, ct); // evict the now-expired entry
            return ResolveResult.Expired(dto);
        }

        return ResolveResult.Found(dto);
    }

    public async Task<UrlStatsDto?> GetStatsAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        code = code.Trim();
        var link = await _repository.GetByCodeAsync(code, ct);
        if (link is null)
            return null;

        var stats = await _analytics.GetStatsAsync(code, ct);
        var daily = stats.Daily.Select(d => new DailyClicksDto(d.Date, d.Count)).ToList();
        return new UrlStatsDto(code, stats.Total, link.CreatedAt, stats.LastClickedAt, daily);
    }

    // --- helpers ---

    private async Task CacheAsync(ShortUrlDto dto, DateTimeOffset now, CancellationToken ct)
    {
        var ttl = ComputeTtl(dto.ExpiresAt, now);
        if (ttl is { } t && t <= TimeSpan.Zero)
            return; // already expired; don't cache

        await _cache.SetAsync(CacheKey(dto.Code), JsonSerializer.Serialize(dto), ttl, ct);
    }

    private static TimeSpan? ComputeTtl(DateTimeOffset? expiresAt, DateTimeOffset now)
    {
        if (expiresAt is not { } e)
            return DefaultCacheTtl;

        var untilExpiry = e - now;
        return untilExpiry < DefaultCacheTtl ? untilExpiry : DefaultCacheTtl;
    }

    private static string CacheKey(string code) => $"url:{code}";

    private static ShortUrlDto? TryDeserialize(string json)
    {
        try { return JsonSerializer.Deserialize<ShortUrlDto>(json); }
        catch (JsonException) { return null; } // treat corrupt cache entries as a miss
    }

    private static string NormalizeAndValidateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ValidationException("URL is required.");

        url = url.Trim();
        if (url.Length > MaxUrlLength)
            throw new ValidationException($"URL exceeds the maximum length of {MaxUrlLength} characters.");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ValidationException("URL must be an absolute http or https URL.");
        }

        return uri.ToString();
    }

    private static void ValidateAlias(string alias)
    {
        if (!AliasPattern().IsMatch(alias))
            throw new ValidationException("Alias must be 3-64 characters using letters, digits, '-' or '_'.");

        if (ReservedCodes.Contains(alias))
            throw new ConflictException($"Alias '{alias}' is reserved.");
    }

    private static void ValidateExpiry(DateTimeOffset? expiresAt, DateTimeOffset now)
    {
        if (expiresAt is { } e && e <= now)
            throw new ValidationException("ExpiresAt must be in the future.");
    }

    private static ShortUrlDto ToDto(ShortUrl e) =>
        new(e.Code!, e.TargetUrl, e.IsCustomAlias, e.CreatedAt, e.ExpiresAt);
}
