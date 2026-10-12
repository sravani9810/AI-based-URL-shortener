using System.Threading.Channels;
using UrlShortener.Domain.Events;
using UrlShortener.Domain.Interfaces;

namespace UrlShortener.Infrastructure.Events;

/// <summary>
/// In-process event bus (default when Kafka is not configured). Publishing writes to an unbounded
/// channel; <see cref="InMemoryClickConsumer"/> drains it. Decouples the redirect from analytics
/// persistence exactly as Kafka would, with zero infrastructure.
/// </summary>
public sealed class InMemoryEventBus : IEventPublisher
{
    private readonly Channel<UrlClickedEvent> _channel =
        Channel.CreateUnbounded<UrlClickedEvent>(new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<UrlClickedEvent> Reader => _channel.Reader;

    public async Task PublishAsync(UrlClickedEvent clickEvent, CancellationToken ct = default) =>
        await _channel.Writer.WriteAsync(clickEvent, ct);
}
