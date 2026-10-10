using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using UrlShortener.Domain.Interfaces;
using UrlShortener.Infrastructure.Caching;
using UrlShortener.Infrastructure.Encoding;
using UrlShortener.Infrastructure.Persistence;

namespace UrlShortener.Infrastructure;

/// <summary>Registers infrastructure services (persistence, caching, encoding) with the DI container.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
                               ?? "Data Source=urlshortener.db";

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IShortUrlRepository, ShortUrlRepository>();
        services.AddSingleton<ICodeGenerator, Base62CodeGenerator>();

        AddCache(services, configuration);

        return services;
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
