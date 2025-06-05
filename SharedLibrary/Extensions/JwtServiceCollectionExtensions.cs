using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Contracts;
using SharedLibrary.Services;

namespace SharedLibrary.Extensions;

public static class JwtServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAuth(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }

    public static IServiceCollection AddSessionCache(this IServiceCollection services)
    {
        services.AddMemoryCache(options =>
        {
            // Configuraciones óptimas para caché de sesiones
            options.SizeLimit = 2048; // 2K entradas máximas
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
        });

        return services;
    }
}
