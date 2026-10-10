using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Services;

/// <summary>Core application service for creating and resolving short links.</summary>
public interface IUrlService
{
    /// <summary>Creates a short link, generating a Base62 code or honoring a validated custom alias.</summary>
    Task<ShortUrlDto> CreateAsync(CreateShortUrlRequest request, CancellationToken ct = default);

    /// <summary>Resolves a code to its destination, or null if not found.</summary>
    Task<ShortUrlDto?> ResolveAsync(string code, CancellationToken ct = default);
}
