namespace UrlShortener.Application.Dtos;

/// <summary>Outcome of resolving a code: found, never existed, or existed but expired.</summary>
public enum ResolveStatus
{
    Found,
    NotFound,
    Expired
}

/// <summary>Resolve outcome plus the link data when available (present for Found and Expired).</summary>
public record ResolveResult(ResolveStatus Status, ShortUrlDto? Url)
{
    public static readonly ResolveResult NotFound = new(ResolveStatus.NotFound, null);
    public static ResolveResult Found(ShortUrlDto url) => new(ResolveStatus.Found, url);
    public static ResolveResult Expired(ShortUrlDto url) => new(ResolveStatus.Expired, url);
}
