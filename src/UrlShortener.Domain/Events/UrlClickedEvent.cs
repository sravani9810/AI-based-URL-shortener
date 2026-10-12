namespace UrlShortener.Domain.Events;

/// <summary>
/// Message published when a short link is resolved on the redirect path. Consumed asynchronously
/// to record analytics without blocking the redirect (ADR-0002).
/// </summary>
public record UrlClickedEvent(string Code, DateTimeOffset ClickedAt, string? Referrer, string? UserAgent);
