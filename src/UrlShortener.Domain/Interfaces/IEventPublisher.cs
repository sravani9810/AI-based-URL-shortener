using UrlShortener.Domain.Events;

namespace UrlShortener.Domain.Interfaces;

/// <summary>
/// Publishes domain events to the analytics pipeline. Implementations (Kafka or in-memory channel)
/// must be fast and best-effort: a publish failure must not break the redirect (ADR-0002).
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(UrlClickedEvent clickEvent, CancellationToken ct = default);
}
