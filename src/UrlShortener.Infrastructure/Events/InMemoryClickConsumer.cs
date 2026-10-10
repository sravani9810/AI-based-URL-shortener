using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UrlShortener.Infrastructure.Events;

/// <summary>Background consumer that drains the <see cref="InMemoryEventBus"/> and persists clicks.</summary>
public sealed class InMemoryClickConsumer : BackgroundService
{
    private readonly InMemoryEventBus _bus;
    private readonly ClickPersistenceHandler _handler;
    private readonly ILogger<InMemoryClickConsumer> _logger;

    public InMemoryClickConsumer(
        InMemoryEventBus bus, ClickPersistenceHandler handler, ILogger<InMemoryClickConsumer> logger)
    {
        _bus = bus;
        _handler = handler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evt in _bus.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _handler.HandleAsync(evt, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Analytics is best-effort; a persistence failure must not crash the consumer.
                _logger.LogError(ex, "Failed to persist click for {Code}", evt.Code);
            }
        }
    }
}
