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
                        "http://go.taxprosuite.com",
                        "https://go.taxprosuite.com",
                        "https://taxprosuite.com",
                        "https://www.taxprosuite.com",
                        "http://taxprosuite.com"
                )
                        // .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .SetIsOriginAllowed(_ => true);
                    ;
                }
            );
        });

        return services;
    }
}
