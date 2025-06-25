using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
