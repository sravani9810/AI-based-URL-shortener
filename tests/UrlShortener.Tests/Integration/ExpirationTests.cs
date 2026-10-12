using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using UrlShortener.Api.Models;
using UrlShortener.Application.Dtos;
using Xunit;

namespace UrlShortener.Tests.Integration;

public class ExpirationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ExpirationTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Expired_link_redirects_then_returns_410_after_expiry()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var expiresAt = _factory.Clock.GetUtcNow().AddMinutes(10);

        var create = await client.PostAsJsonAsync("/api/urls",
            new CreateShortUrlRequest("https://example.com/temp", ExpiresAt: expiresAt));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var body = await create.Content.ReadFromJsonAsync<ShortUrlResponse>();

        // Before expiry: redirects.
        var before = await client.GetAsync($"/{body!.Code}");
        Assert.Equal(HttpStatusCode.Found, before.StatusCode);

        // Advance the server clock past expiry.
        _factory.Clock.Advance(TimeSpan.FromMinutes(11));

        var after = await client.GetAsync($"/{body.Code}");
        Assert.Equal(HttpStatusCode.Gone, after.StatusCode); // 410
    }

    [Fact]
    public async Task Create_with_past_expiry_returns_400()
    {
        var client = _factory.CreateClient();
        var past = _factory.Clock.GetUtcNow().AddMinutes(-5);

        var response = await client.PostAsJsonAsync("/api/urls",
            new CreateShortUrlRequest("https://example.com", ExpiresAt: past));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
