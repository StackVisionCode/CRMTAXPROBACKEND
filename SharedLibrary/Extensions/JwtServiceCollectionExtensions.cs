using System.Runtime.Intrinsics.Arm;
using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedLibrary.Contracts;
using SharedLibrary.Contracts.Security;
using SharedLibrary.Services;
using SharedLibrary.Services.ConfirmAccountService;
using SharedLibrary.Services.Security;
using SharedLibrary.Services.SignatureToken;

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
        services.AddScoped<IResetTokenService, ResetTokenService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IConfirmTokenService, ConfirmTokenService>();
        services.AddScoped<ISignatureValidToken, SignatureValidToken>();
        services.AddScoped<IPasswordHash, PasswordHash>();
        services.AddSingleton<IEncryptionService, AesEncryptionService>();

        return services;
    }

    /// <summary>
    /// ✅ MÉTODO CORREGIDO - Delega la configuración del caché al nuevo sistema HybridCache,
    /// manteniendo un fallback a un caché en memoria simple si la configuración avanzada falla.
    /// </summary>
    public static IServiceCollection AddSessionCache(this IServiceCollection services)
    {
        // El service provider se necesita para acceder a la configuración y los logs.
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = serviceProvider.GetRequiredService<ILogger<IServiceCollection>>();

        try
        {
            // Intentar usar el nuevo sistema de caché híbrido, que es el método preferido.
            logger.LogInformation("Inicializando sistema de caché a través de AddHybridCache.");
            services.AddHybridCache(configuration);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Falló la inicialización de AddHybridCache. Se usará un fallback a AddMemoryCache simple."
            );

            // Fallback a la implementación anterior si el sistema híbrido falla catastróficamente.
            // Esto llama al método AddSessionCache de la otra clase, que solo configura IMemoryCache.
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = 2048;
                options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
            });
        }

        return services;
    }
}
