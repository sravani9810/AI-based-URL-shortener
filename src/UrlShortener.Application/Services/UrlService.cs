using System.Text.RegularExpressions;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Exceptions;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Interfaces;

namespace UrlShortener.Application.Services;

/// <summary>
/// Creates and resolves short links. Enforces URL/alias validation, assigns Base62 codes from the
/// row id for auto links (ADR-0007), and handles custom-alias conflicts.
/// </summary>
public sealed partial class UrlService : IUrlService
{
    private const int MaxUrlLength = 2048;

    // Aliases must be 3-64 chars of url-safe characters. Also blocks collisions with top-level routes.
    [GeneratedRegex("^[A-Za-z0-9_-]{3,64}$")]
    private static partial Regex AliasPattern();

    private static readonly HashSet<string> ReservedCodes =
        new(StringComparer.OrdinalIgnoreCase) { "api", "healthz", "swagger", "favicon.ico", "index.html" };

    private readonly IShortUrlRepository _repository;
    private readonly ICodeGenerator _codeGenerator;
    private readonly TimeProvider _timeProvider;

    public UrlService(IShortUrlRepository repository, ICodeGenerator codeGenerator, TimeProvider timeProvider)
    {
        _repository = repository;
        _codeGenerator = codeGenerator;
        _timeProvider = timeProvider;
    }

    public async Task<ShortUrlDto> CreateAsync(CreateShortUrlRequest request, CancellationToken ct = default)
    {
        var targetUrl = NormalizeAndValidateUrl(request.Url);
        var now = _timeProvider.GetUtcNow();

        if (!string.IsNullOrWhiteSpace(request.Alias))
        {
            var alias = request.Alias.Trim();
            ValidateAlias(alias);
            if (await _repository.CodeExistsAsync(alias, ct))
                throw new ConflictException($"Alias '{alias}' is already in use.");

            var custom = await _repository.AddAsync(new ShortUrl
            {
                Code = alias,
                TargetUrl = targetUrl,
                IsCustomAlias = true,
                CreatedAt = now
            }, ct);

            return ToDto(custom);
        }

        // Auto code: insert first to obtain the monotonic id, then derive the Base62 code.
        var entity = await _repository.AddAsync(new ShortUrl
        {
            Code = null,
            TargetUrl = targetUrl,
            IsCustomAlias = false,
            CreatedAt = now
        }, ct);

        entity.Code = _codeGenerator.Encode(entity.Id);
        await _repository.UpdateAsync(entity, ct);

        return ToDto(entity);
    }

    public async Task<ShortUrlDto?> ResolveAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var entity = await _repository.GetByCodeAsync(code.Trim(), ct);
        return entity is null ? null : ToDto(entity);
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

    private static ShortUrlDto ToDto(ShortUrl e) =>
        new(e.Code!, e.TargetUrl, e.IsCustomAlias, e.CreatedAt);
}
