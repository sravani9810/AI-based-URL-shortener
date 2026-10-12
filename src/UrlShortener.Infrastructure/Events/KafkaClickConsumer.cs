using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UrlShortener.Domain.Events;

namespace UrlShortener.Infrastructure.Events;

/// <summary>Background consumer that reads click events from Kafka and persists them.</summary>
public sealed class KafkaClickConsumer : BackgroundService
{
    private readonly string _bootstrapServers;
    private readonly string _topic;
    private readonly ClickPersistenceHandler _handler;
    private readonly ILogger<KafkaClickConsumer> _logger;

    public KafkaClickConsumer(
        string bootstrapServers, string topic, ClickPersistenceHandler handler,
        ILogger<KafkaClickConsumer> logger)
    {
        _bootstrapServers = bootstrapServers;
        _topic = topic;
        _handler = handler;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Kafka's Consume is blocking; run it on a dedicated background task.
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private async Task ConsumeLoop(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = "url-shortener-analytics",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(_topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(500));
                    if (result?.Message?.Value is null)
                        continue;

                    var evt = JsonSerializer.Deserialize<UrlClickedEvent>(result.Message.Value);
                    if (evt is not null)
                        await _handler.HandleAsync(evt, stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogWarning(ex, "Kafka consume error; continuing.");
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Skipping malformed click message.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
        finally
        {
            consumer.Close();
        }
    }
}
