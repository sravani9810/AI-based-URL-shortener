using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Events;
using UrlShortener.Domain.Interfaces;

namespace UrlShortener.Infrastructure.Events;

/// <summary>
/// Shared handler that persists a <see cref="UrlClickedEvent"/> as a <see cref="ClickEvent"/> row.
/// Used by both the in-memory and Kafka consumers. Creates a DI scope per event because the
/// analytics repository (and its DbContext) is scoped.
/// </summary>
public sealed class ClickPersistenceHandler
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ClickPersistenceHandler(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task HandleAsync(UrlClickedEvent evt, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IClickAnalyticsRepository>();

        await repo.AddAsync(new ClickEvent
        {
            Code = evt.Code,
            ClickedAt = evt.ClickedAt,
            Referrer = evt.Referrer,
            UserAgent = evt.UserAgent
        }, ct);
    }
}
