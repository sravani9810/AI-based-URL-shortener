namespace UrlShortener.Domain.Entities;

/// <summary>
/// A single recorded click on a short link. Deliberately low-PII: no raw IP is stored
/// (ADR-0008). Persisted asynchronously by the analytics consumer, off the redirect hot path.
/// </summary>
public class ClickEvent
{
    public long Id { get; set; }

    /// <summary>The short code that was clicked.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>When the click happened (UTC).</summary>
    public DateTimeOffset ClickedAt { get; set; }

    /// <summary>Referer header, if present.</summary>
    public string? Referrer { get; set; }

    /// <summary>User-Agent header, if present.</summary>
    public string? UserAgent { get; set; }
}
