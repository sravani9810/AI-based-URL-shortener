using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using UrlShortener.Domain.Interfaces;
using UrlShortener.Infrastructure.Caching;
using UrlShortener.Infrastructure.Encoding;
using UrlShortener.Infrastructure.Events;
using UrlShortener.Infrastructure.Persistence;

namespace UrlShortener.Infrastructure;

/// <summary>Registers infrastructure services (persistence, caching, events, encoding) with the DI container.</summary>
public static class DependencyInjection
{
    private const string ClickTopic = "url-clicks";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("Default");

        services.AddDbContext<AppDbContext>(options =>
        {
            if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
                options.UseNpgsql(connectionString);
            else
                options.UseSqlite(connectionString ?? DefaultSqliteConnectionString());
        });
        services.AddScoped<IShortUrlRepository, ShortUrlRepository>();
        services.AddScoped<IClickAnalyticsRepository, ClickAnalyticsRepository>();

        // Codes = sequential id → keyed Feistel permutation → Base62, so they are non-enumerable
        // while staying collision-free (ADR-0007). Set ShortCode:Secret per environment.
        var codeSecret = configuration["ShortCode:Secret"] ?? "url-shortener-dev-secret";
        services.AddSingleton<Base62CodeGenerator>();
        services.AddSingleton<ICodeGenerator>(sp =>
            new FeistelCodeGenerator(sp.GetRequiredService<Base62CodeGenerator>(), codeSecret));

        AddCache(services, configuration);
        AddEvents(services, configuration);

        return services;
    }

    /// <summary>
    /// Uses a Kafka producer/consumer when bootstrap servers are configured, otherwise an in-process
    /// channel bus. Either way the redirect publishes <c>UrlClicked</c> and a background consumer
    /// persists it, so the hot path is decoupled from analytics writes (ADR-0002).
    /// </summary>
    private static void AddEvents(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ClickPersistenceHandler>();

        var kafkaServers = configuration.GetConnectionString("Kafka")
                           ?? configuration["Kafka:BootstrapServers"];

        if (string.IsNullOrWhiteSpace(kafkaServers))
        {
            services.AddSingleton<InMemoryEventBus>();
            services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<InMemoryEventBus>());
            services.AddHostedService<InMemoryClickConsumer>();
            return;
        }

        services.AddSingleton<IEventPublisher>(sp => new KafkaEventPublisher(
            kafkaServers, ClickTopic, sp.GetRequiredService<ILogger<KafkaEventPublisher>>()));
        services.AddHostedService(sp => new KafkaClickConsumer(
            kafkaServers, ClickTopic,
            sp.GetRequiredService<ClickPersistenceHandler>(),
            sp.GetRequiredService<ILogger<KafkaClickConsumer>>()));
    }

    /// <summary>
    /// Default SQLite location when nothing is configured: a per-user app-data folder, so a
    /// double-clicked desktop executable always has a writable, persistent database path.
    /// </summary>
    private static string DefaultSqliteConnectionString()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UrlShortener");
        Directory.CreateDirectory(dir);
        return $"Data Source={Path.Combine(dir, "urlshortener.db")}";
    }

    /// <summary>
    /// Uses Redis when a connection string is configured, otherwise an in-process cache. The Redis
    /// connection is created with abortConnect=false so startup succeeds even if Redis is briefly
    /// unavailable; per-operation failures degrade to misses (see <see cref="RedisCacheStore"/>).
    /// </summary>
    private static void AddCache(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis")
                              ?? configuration["Redis:ConnectionString"];

        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<ICacheStore, InMemoryCacheStore>();
            return;
        }

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse(redisConnection);
            options.AbortOnConnectFail = false;
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Redis");
            try
            {
                return ConnectionMultiplexer.Connect(options);
            }
            catch (RedisConnectionException ex)
            {
                logger.LogWarning(ex, "Initial Redis connection failed; will retry lazily.");
                return ConnectionMultiplexer.Connect(options); // multiplexer still usable; reconnects in background
            }
        });
        services.AddSingleton<ICacheStore, RedisCacheStore>();
    }
}
