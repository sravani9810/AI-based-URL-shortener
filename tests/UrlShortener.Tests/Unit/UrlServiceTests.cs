using UrlShortener.Application.Dtos;
using UrlShortener.Application.Exceptions;
using UrlShortener.Application.Services;
using UrlShortener.Infrastructure.Encoding;
using UrlShortener.Tests.Fakes;
using Xunit;

namespace UrlShortener.Tests.Unit;

public class UrlServiceTests
{
    private readonly FakeShortUrlRepository _repo = new();
    private readonly UrlService _sut;

    public UrlServiceTests() =>
        _sut = new UrlService(_repo, new Base62CodeGenerator(), TimeProvider.System);

    [Fact]
    public async Task CreateAsync_assigns_base62_code_from_id_when_no_alias()
    {
        var result = await _sut.CreateAsync(new CreateShortUrlRequest("https://example.com/page"));

        Assert.False(result.IsCustomAlias);
        Assert.Equal("1", result.Code); // first id -> Base62(1)
        Assert.Equal("https://example.com/page", result.TargetUrl);
    }

    [Fact]
    public async Task CreateAsync_honors_custom_alias()
    {
        var result = await _sut.CreateAsync(new CreateShortUrlRequest("https://example.com", "my-link"));

        Assert.True(result.IsCustomAlias);
        Assert.Equal("my-link", result.Code);
    }

    [Fact]
    public async Task CreateAsync_duplicate_alias_throws_conflict()
    {
        await _sut.CreateAsync(new CreateShortUrlRequest("https://example.com", "dup"));

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CreateAsync(new CreateShortUrlRequest("https://other.com", "dup")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("javascript:alert(1)")]
    public async Task CreateAsync_invalid_url_throws_validation(string url) =>
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateAsync(new CreateShortUrlRequest(url)));

    [Theory]
    [InlineData("ab")]          // too short
    [InlineData("has space")]   // invalid char
    [InlineData("api")]         // reserved
    public async Task CreateAsync_invalid_alias_is_rejected(string alias) =>
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _sut.CreateAsync(new CreateShortUrlRequest("https://example.com", alias)));

    [Fact]
    public async Task ResolveAsync_returns_null_for_unknown_code() =>
        Assert.Null(await _sut.ResolveAsync("nope"));

    [Fact]
    public async Task ResolveAsync_returns_dto_for_known_code()
    {
        var created = await _sut.CreateAsync(new CreateShortUrlRequest("https://example.com"));
        var resolved = await _sut.ResolveAsync(created.Code);

        Assert.NotNull(resolved);
        Assert.Equal("https://example.com/", resolved!.TargetUrl);
    }
}
