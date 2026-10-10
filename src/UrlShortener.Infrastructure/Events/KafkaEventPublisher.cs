using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using UrlShortener.Domain.Events;
using UrlShortener.Domain.Interfaces;

namespace UrlShortener.Infrastructure.Events;

/// <summary>
/// Kafka producer for click events. Uses fire-and-forget <see cref="IProducer{TKey,TValue}.Produce"/>
/// (not awaited ProduceAsync) so the redirect hot path never blocks on broker acknowledgement.
/// Produce failures are logged, not thrown (ADR-0002).
/// </summary>
public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly string _topic;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(string bootstrapServers, string topic, ILogger<KafkaEventPublisher> logger)
    {
        _topic = topic;
        _logger = logger;
        _producer = new ProducerBuilder<Null, string>(
            new ProducerConfig { BootstrapServers = bootstrapServers }).Build();
    }

    public Task PublishAsync(UrlClickedEvent clickEvent, CancellationToken ct = default)
    {
        try
        {
            var payload = JsonSerializer.Serialize(clickEvent);
            _producer.Produce(_topic, new Message<Null, string> { Value = payload });
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogWarning(ex, "Kafka produce failed for {Code}; click not recorded.", clickEvent.Code);
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
