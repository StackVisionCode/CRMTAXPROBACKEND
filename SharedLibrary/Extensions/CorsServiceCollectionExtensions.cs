using Microsoft.Extensions.DependencyInjection;

namespace SharedLibrary.Extensions;

public static class CorsServiceCollectionExtensions
{
    public static IServiceCollection AddCustomCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(
                "AllowAll",
                policy =>
                {
                    policy
                        .WithOrigins(
                            "http://localhost:4200",
                            "http://127.0.0.1:5500",
                            "http://localhost:5500"
                        )
                        // .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .SetIsOriginAllowed(_ => true);;
                }
            );
        });

        return services;
    }
}
