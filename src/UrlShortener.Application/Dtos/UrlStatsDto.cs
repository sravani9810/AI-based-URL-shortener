namespace UrlShortener.Application.Dtos;

/// <summary>Click count for one UTC day.</summary>
public record DailyClicksDto(DateOnly Date, long Count);

/// <summary>Analytics summary for a short link.</summary>
/// <param name="Code">The short code.</param>
/// <param name="TotalClicks">Total clicks recorded.</param>
/// <param name="CreatedAt">When the link was created (UTC).</param>
/// <param name="LastClickedAt">Most recent click (UTC), or null if never clicked.</param>
/// <param name="ClicksPerDay">Per-day click counts, ascending by date.</param>
public record UrlStatsDto(
    string Code,
    long TotalClicks,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastClickedAt,
    IReadOnlyList<DailyClicksDto> ClicksPerDay);
