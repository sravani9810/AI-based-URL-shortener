using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Application.Services;

namespace UrlShortener.Application;

/// <summary>Registers application-layer services with the DI container.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IUrlService, UrlService>();
        return services;
    }
}
