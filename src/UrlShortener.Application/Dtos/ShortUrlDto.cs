namespace UrlShortener.Application.Dtos;

/// <summary>Result of creating/resolving a short link (host-agnostic; the full short URL is composed by the API layer).</summary>
/// <param name="Code">The public short code.</param>
/// <param name="TargetUrl">The destination URL.</param>
/// <param name="IsCustomAlias">Whether the code was a caller-supplied alias.</param>
/// <param name="CreatedAt">Creation timestamp (UTC).</param>
public record ShortUrlDto(string Code, string TargetUrl, bool IsCustomAlias, DateTimeOffset CreatedAt);
