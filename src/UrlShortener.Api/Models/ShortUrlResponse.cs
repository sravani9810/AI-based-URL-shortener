namespace UrlShortener.Api.Models;

/// <summary>API response describing a short link.</summary>
/// <param name="Code">The public short code.</param>
/// <param name="ShortUrl">The fully-qualified short URL (scheme + host + code).</param>
/// <param name="TargetUrl">The destination URL.</param>
/// <param name="IsCustomAlias">Whether the code was a caller-supplied alias.</param>
/// <param name="CreatedAt">Creation timestamp (UTC).</param>
public record ShortUrlResponse(
    string Code,
    string ShortUrl,
    string TargetUrl,
    bool IsCustomAlias,
    DateTimeOffset CreatedAt);
