using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace UrlShortener.Tests.Integration;

/// <summary>
/// Boots the real API against an isolated, per-instance SQLite file so integration tests exercise
/// the full pipeline (routing, DI, EF Core, migrations) end-to-end. Swaps in a controllable clock
/// so time-dependent behaviour (expiry) can be tested without real delays.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"urlshortener-test-{Guid.NewGuid():N}.db");

    public FakeTimeProvider Clock { get; } =
        new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
        builder.UseSetting("App:OpenBrowser", "false"); // never launch a browser from the test host

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}
