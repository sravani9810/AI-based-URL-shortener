using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Domain.Interfaces;
using UrlShortener.Infrastructure.Encoding;
using UrlShortener.Infrastructure.Persistence;

namespace UrlShortener.Infrastructure;

/// <summary>Registers infrastructure services (persistence, encoding) with the DI container.</summary>
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

        return services;
    }
}
