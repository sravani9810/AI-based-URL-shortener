namespace UrlShortener.Domain.Analytics;

/// <summary>Click count for a single calendar day (UTC).</summary>
public record DailyClickCount(DateOnly Date, long Count);

/// <summary>Aggregated click analytics for a short link.</summary>
public record ClickStats(long Total, DateTimeOffset? LastClickedAt, IReadOnlyList<DailyClickCount> Daily);
