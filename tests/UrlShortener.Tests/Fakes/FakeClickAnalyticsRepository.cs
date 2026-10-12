using UrlShortener.Domain.Analytics;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Interfaces;

namespace UrlShortener.Tests.Fakes;

/// <summary>In-memory analytics repository fake for service tests.</summary>
public sealed class FakeClickAnalyticsRepository : IClickAnalyticsRepository
{
    private readonly List<ClickEvent> _clicks = new();

    public Task AddAsync(ClickEvent click, CancellationToken ct = default)
    {
        _clicks.Add(click);
        return Task.CompletedTask;
    }

    public Task<ClickStats> GetStatsAsync(string code, CancellationToken ct = default)
    {
        var times = _clicks.Where(c => c.Code == code).Select(c => c.ClickedAt).ToList();
        if (times.Count == 0)
            return Task.FromResult(new ClickStats(0, null, Array.Empty<DailyClickCount>()));

        var daily = times
            .GroupBy(t => DateOnly.FromDateTime(t.UtcDateTime))
            .Select(g => new DailyClickCount(g.Key, g.LongCount()))
            .OrderBy(d => d.Date)
            .ToList();

        return Task.FromResult(new ClickStats(times.Count, times.Max(), daily));
    }
}
