using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Authorizations;

namespace SharedLibrary.Extensions;

public static class RbacServiceCollectionExtensions
{
    public static IServiceCollection AddRbac(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        // HttpClient apuntando al microservicio de Auth
        services.AddHttpClient(
            "AuthService",
            (sp, c) =>
            {
                var url =
                    config["Services:Auth"]
                    ?? throw new InvalidOperationException(
                        "'Services:Auth' no está configurado (appsettings)"
                    );

                c.BaseAddress = new Uri(url); // p. ej. http://localhost:5001
                c.DefaultRequestHeaders.Add("X-From-Gateway", "Api-Gateway");
            }
        );

        // Infra de autorización
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionHandler>();

        services.AddHttpContextAccessor();

        // Política (si ya existe AddAuthorization en Program no pasa nada: se complementa)
        services.AddAuthorization();

        return services;
    }
}
