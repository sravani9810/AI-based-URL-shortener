using Microsoft.EntityFrameworkCore;
using UrlShortener.Domain.Analytics;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Interfaces;

namespace UrlShortener.Infrastructure.Persistence;

/// <summary>EF Core-backed click analytics store.</summary>
/// <remarks>
/// Aggregation is done in-memory after fetching timestamps for a code. This is intentionally simple
/// for a prototype; at scale it would be replaced by pre-aggregated counters or a time-series store
/// (documented in limitations).
/// </remarks>
public class ClickAnalyticsRepository : IClickAnalyticsRepository
{
    private readonly AppDbContext _db;

    public ClickAnalyticsRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(ClickEvent click, CancellationToken ct = default)
    {
        _db.ClickEvents.Add(click);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ClickStats> GetStatsAsync(string code, CancellationToken ct = default)
    {
        var timestamps = await _db.ClickEvents
            .AsNoTracking()
            .Where(c => c.Code == code)
            .Select(c => c.ClickedAt)
            .ToListAsync(ct);

        if (timestamps.Count == 0)
            return new ClickStats(0, null, Array.Empty<DailyClickCount>());

        var daily = timestamps
            .GroupBy(t => DateOnly.FromDateTime(t.UtcDateTime))
            .Select(g => new DailyClickCount(g.Key, g.LongCount()))
            .OrderBy(d => d.Date)
            .ToList();

        return new ClickStats(timestamps.Count, timestamps.Max(), daily);
    }
}
