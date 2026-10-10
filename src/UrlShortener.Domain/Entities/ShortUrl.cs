namespace UrlShortener.Domain.Entities;

/// <summary>
/// A single short-link mapping. The <see cref="Code"/> is the public key used in the
/// short URL path; <see cref="TargetUrl"/> is where it redirects. For auto-generated codes
/// the code is the Base62 encoding of <see cref="Id"/> (ADR-0007).
/// </summary>
public class ShortUrl
{
    /// <summary>Monotonic primary key. Source of the Base62 code for non-custom links.</summary>
    public long Id { get; set; }

    /// <summary>
    /// Public short code. Null only transiently between insert and code assignment for
    /// auto-generated links; always set once persisted.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>Absolute destination URL (http/https), validated before persistence.</summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>True when the code was supplied by the caller as a custom alias.</summary>
    public bool IsCustomAlias { get; set; }

    /// <summary>Creation timestamp (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
