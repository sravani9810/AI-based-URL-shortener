namespace UrlShortener.Application.Dtos;

/// <summary>Request to create a short link.</summary>
/// <param name="Url">Absolute destination URL (http/https).</param>
/// <param name="Alias">Optional caller-chosen custom code.</param>
public record CreateShortUrlRequest(string Url, string? Alias = null);
