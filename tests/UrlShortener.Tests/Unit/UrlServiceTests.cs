using Microsoft.Extensions.Time.Testing;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Exceptions;
using UrlShortener.Application.Services;
using UrlShortener.Infrastructure.Caching;
using UrlShortener.Infrastructure.Encoding;
using UrlShortener.Tests.Fakes;
using Xunit;

namespace UrlShortener.Tests.Unit;

public class UrlServiceTests
{
    private readonly FakeShortUrlRepository _repo = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    private readonly UrlService _sut;

    public UrlServiceTests() =>
        _sut = new UrlService(_repo, new Base62CodeGenerator(), new InMemoryCacheStore(), _clock);

    [Fact]
    public async Task CreateAsync_assigns_base62_code_from_id_when_no_alias()
    {
        var result = await _sut.CreateAsync(new CreateShortUrlRequest("https://example.com/page"));

        Assert.False(result.IsCustomAlias);
        Assert.Equal("1", result.Code);
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
    [InlineData("ab")]
    [InlineData("has space")]
    [InlineData("api")]
    public async Task CreateAsync_invalid_alias_is_rejected(string alias) =>
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _sut.CreateAsync(new CreateShortUrlRequest("https://example.com", alias)));

    [Fact]
    public async Task CreateAsync_past_expiry_throws_validation()
    {
        var past = _clock.GetUtcNow().AddMinutes(-1);
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateAsync(new CreateShortUrlRequest("https://example.com", ExpiresAt: past)));
    }

    [Fact]
    public async Task ResolveAsync_returns_notfound_for_unknown_code()
    {
        var result = await _sut.ResolveAsync("nope");
        Assert.Equal(ResolveStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task ResolveAsync_returns_found_for_known_code()
    {
        var created = await _sut.CreateAsync(new CreateShortUrlRequest("https://example.com"));

        var result = await _sut.ResolveAsync(created.Code);

        Assert.Equal(ResolveStatus.Found, result.Status);
        Assert.Equal("https://example.com/", result.Url!.TargetUrl);
    }

    [Fact]
    public async Task ResolveAsync_returns_expired_after_expiry_passes()
    {
        var created = await _sut.CreateAsync(
            new CreateShortUrlRequest("https://example.com", ExpiresAt: _clock.GetUtcNow().AddMinutes(5)));

        // Before expiry: found.
        Assert.Equal(ResolveStatus.Found, (await _sut.ResolveAsync(created.Code)).Status);

        // Advance past expiry: gone.
        _clock.Advance(TimeSpan.FromMinutes(6));
        Assert.Equal(ResolveStatus.Expired, (await _sut.ResolveAsync(created.Code)).Status);
    }

    [Fact]
    public async Task ResolveAsync_serves_from_cache_after_repository_row_removed()
    {
        var created = await _sut.CreateAsync(new CreateShortUrlRequest("https://cached.example"));
        // Create write-through cached it; wipe the backing store to prove the next read is a cache hit.
        _repo.Clear();

        var result = await _sut.ResolveAsync(created.Code);

        Assert.Equal(ResolveStatus.Found, result.Status);
        Assert.Equal("https://cached.example/", result.Url!.TargetUrl);
    }
}
