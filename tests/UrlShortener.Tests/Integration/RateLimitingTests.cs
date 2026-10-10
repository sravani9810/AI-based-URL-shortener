using System.Net;
using System.Net.Http.Json;
using UrlShortener.Application.Dtos;
using Xunit;

namespace UrlShortener.Tests.Integration;

public class RateLimitingTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public RateLimitingTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Create_endpoint_returns_429_when_token_bucket_is_exhausted()
    {
        var client = _factory.CreateClient();

        // Bucket is 20 tokens; fire 25 rapid requests and expect at least one 429.
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 25; i++)
        {
            var response = await client.PostAsJsonAsync("/api/urls",
                new CreateShortUrlRequest($"https://example.com/{i}"));
            statuses.Add(response.StatusCode);
        }

        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
        Assert.True(statuses.Count(s => s == HttpStatusCode.Created) <= 20,
            "No more than the token limit should succeed.");
    }
}
