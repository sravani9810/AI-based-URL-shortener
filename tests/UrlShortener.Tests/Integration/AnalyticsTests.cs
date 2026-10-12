using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using UrlShortener.Api.Models;
using UrlShortener.Application.Dtos;
using Xunit;

namespace UrlShortener.Tests.Integration;

public class AnalyticsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AnalyticsTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Clicks_are_recorded_through_the_event_pipeline()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var create = await client.PostAsJsonAsync("/api/urls",
            new CreateShortUrlRequest("https://example.com/analytics"));
        var body = await create.Content.ReadFromJsonAsync<ShortUrlResponse>();
        var code = body!.Code;

        // Three redirects → three click events published and consumed asynchronously.
        for (var i = 0; i < 3; i++)
            await client.GetAsync($"/{code}");

        var stats = await PollStatsUntilAsync(client, code, expected: 3);

        Assert.NotNull(stats);
        Assert.Equal(3, stats!.TotalClicks);
        Assert.NotNull(stats.LastClickedAt);
        Assert.Single(stats.ClicksPerDay);
        Assert.Equal(3, stats.ClicksPerDay[0].Count);
    }

    [Fact]
    public async Task Stats_for_unknown_code_returns_404()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/urls/nope/stats");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<UrlStatsDto?> PollStatsUntilAsync(
        HttpClient client, string code, int expected, int timeoutMs = 3000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        UrlStatsDto? stats = null;
        while (DateTime.UtcNow < deadline)
        {
            stats = await client.GetFromJsonAsync<UrlStatsDto>($"/api/urls/{code}/stats");
            if (stats is not null && stats.TotalClicks >= expected)
                return stats;
            await Task.Delay(50);
        }
        return stats;
    }
}
