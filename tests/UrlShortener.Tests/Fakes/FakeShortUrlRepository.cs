using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Interfaces;

namespace UrlShortener.Tests.Fakes;

/// <summary>In-memory repository fake that mimics identity assignment for fast, isolated service tests.</summary>
public sealed class FakeShortUrlRepository : IShortUrlRepository
{
    private readonly List<ShortUrl> _store = new();
    private long _nextId;

    public Task<ShortUrl?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        Task.FromResult(_store.FirstOrDefault(u => u.Code == code));

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct = default) =>
        Task.FromResult(_store.Any(u => u.Code == code));

    public Task<ShortUrl> AddAsync(ShortUrl shortUrl, CancellationToken ct = default)
    {
        shortUrl.Id = ++_nextId;
        _store.Add(shortUrl);
        return Task.FromResult(shortUrl);
    }

    public Task UpdateAsync(ShortUrl shortUrl, CancellationToken ct = default) => Task.CompletedTask;
}
