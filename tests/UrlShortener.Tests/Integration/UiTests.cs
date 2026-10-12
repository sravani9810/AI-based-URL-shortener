using System.Net;
using Xunit;

namespace UrlShortener.Tests.Integration;

public class UiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public UiTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Root_serves_the_embedded_ui()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("URL Shortener", html);
    }
}
