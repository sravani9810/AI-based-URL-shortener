using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Exceptions;
using UrlShortener.Infrastructure.Persistence;
using Xunit;

namespace UrlShortener.Tests.Integration;

public class RepositoryConflictTests
{
    [Fact]
    public async Task Inserting_a_duplicate_code_throws_DuplicateCodeException()
    {
        // Real EF Core + SQLite so the unique index actually fires.
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var db = new AppDbContext(options);
            await db.Database.EnsureCreatedAsync();
            var repo = new ShortUrlRepository(db);

            await repo.AddAsync(new ShortUrl { Code = "dup", TargetUrl = "https://a.example" });

            await Assert.ThrowsAsync<DuplicateCodeException>(() =>
                repo.AddAsync(new ShortUrl { Code = "dup", TargetUrl = "https://b.example" }));
        }
        finally
        {
            connection.Close();
        }
    }
}
