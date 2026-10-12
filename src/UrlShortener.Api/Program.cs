using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Controllers;
using UrlShortener.Api.Infrastructure;
using UrlShortener.Application;
using UrlShortener.Infrastructure;
using UrlShortener.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// When run as a standalone desktop executable (no ASPNETCORE_URLS, not in a container), bind a local
// port: prefer 5080, but fall back to a free OS-assigned port if it is already in use, so the app
// never crashes on a port conflict. The browser is opened to the actual bound address below.
var runningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
if (!runningInContainer
    && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"))
    && builder.Configuration["urls"] is null)
{
    builder.WebHost.UseUrls($"http://localhost:{ChooseLocalPort(5080)}");
}

// --- Services ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

// Token-bucket rate limiting on the create endpoint, partitioned by client IP (abuse protection).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitingPolicies.Create, httpContext =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 20,
                TokensPerPeriod = 20,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();

// --- Initialize the database on startup (prototype convenience; a real deployment gates this) ---
// SQLite has migration history; Postgres (compose) is created directly from the model. See ADR-0003.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsNpgsql())
        await db.Database.EnsureCreatedAsync();
    else
        await db.Database.MigrateAsync();
}

// --- Pipeline ---
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();

// Serve the embedded single-page UI at the root. The GET /{code} redirect only matches a single
// non-empty segment, so it never collides with "/".
var indexHtml = LoadEmbeddedIndexHtml();
app.MapGet("/", () => Results.Content(indexHtml, "text/html"));

app.MapControllers();
app.MapHealthChecks("/healthz");

// Auto-open the browser to the UI when launched as a desktop app. Enabled by default except in a
// container or test host; override with App:OpenBrowser. Best-effort — the URL is printed regardless.
var openBrowser = builder.Configuration.GetValue<bool?>("App:OpenBrowser")
                  ?? (!runningInContainer && !app.Environment.IsEnvironment("Testing"));
if (openBrowser)
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var address = app.Urls.FirstOrDefault() ?? "http://localhost:5080";
        Console.WriteLine($"\n  URL Shortener is running at {address}\n  Opening your browser...\n");
        TryOpenBrowser(address);
    });
}

app.Run();

// Returns the preferred port if free, otherwise an OS-assigned free port (avoids a hard crash when
// the default port is already in use).
static int ChooseLocalPort(int preferred)
{
    if (IsPortFree(preferred))
        return preferred;

    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}

static bool IsPortFree(int port)
{
    try
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        listener.Stop();
        return true;
    }
    catch (SocketException)
    {
        return false;
    }
}

// Loads the UI page embedded in the assembly (so the published binary needs no wwwroot folder).
static string LoadEmbeddedIndexHtml()
{
    var assembly = typeof(Program).Assembly;
    using var stream = assembly.GetManifestResourceStream("index.html")
                       ?? throw new InvalidOperationException("Embedded UI resource 'index.html' not found.");
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
}

// Opens the default browser cross-platform; failures are ignored (the URL is already printed).
static void TryOpenBrowser(string url)
{
    try
    {
        if (OperatingSystem.IsWindows())
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        else if (OperatingSystem.IsMacOS())
            Process.Start("open", url);
        else if (OperatingSystem.IsLinux())
            Process.Start("xdg-open", url);
    }
    catch
    {
        // Headless or no browser available — the printed URL is the fallback.
    }
}

// Exposed so the integration-test host (WebApplicationFactory) can reference the entry point.
public partial class Program { }
