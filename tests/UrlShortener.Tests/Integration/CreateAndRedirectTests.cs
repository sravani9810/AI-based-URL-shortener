using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using UrlShortener.Api.Models;
using UrlShortener.Application.Dtos;
using Xunit;

namespace UrlShortener.Tests.Integration;

public class CreateAndRedirectTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public CreateAndRedirectTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Create_then_redirect_round_trips()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Create
        var create = await client.PostAsJsonAsync("/api/urls",
            new CreateShortUrlRequest("https://example.com/landing"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var body = await create.Content.ReadFromJsonAsync<ShortUrlResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Code));

        // Redirect
        var redirect = await client.GetAsync($"/{body.Code}");
        Assert.Equal(HttpStatusCode.Found, redirect.StatusCode); // 302
        Assert.Equal("https://example.com/landing", redirect.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Redirect_unknown_code_returns_404()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/does-not-exist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_invalid_url_returns_400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/urls",
            new CreateShortUrlRequest("not-a-url"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_duplicate_alias_returns_409()
    {
        var client = _factory.CreateClient();

        var first = await client.PostAsJsonAsync("/api/urls",
            new CreateShortUrlRequest("https://example.com", "launch"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/urls",
            new CreateShortUrlRequest("https://example.org", "launch"));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
}
