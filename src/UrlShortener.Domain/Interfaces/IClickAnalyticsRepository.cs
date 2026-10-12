using UrlShortener.Domain.Analytics;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Domain.Interfaces;

/// <summary>Persistence + aggregation for click analytics.</summary>
public interface IClickAnalyticsRepository
{
    /// <summary>Records a single click.</summary>
    Task AddAsync(ClickEvent click, CancellationToken ct = default);

    /// <summary>Returns aggregated stats (total, last-clicked, per-day counts) for a code.</summary>
    Task<ClickStats> GetStatsAsync(string code, CancellationToken ct = default);
}
