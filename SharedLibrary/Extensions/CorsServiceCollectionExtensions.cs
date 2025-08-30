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
                        .WithOrigins("http://localhost:4200",
                        "http://app.taxprosuite.com",
                        "https://app.taxprosuite.com",
                        "https://taxprosuite.com",
                        "https://www.taxprosuite.com",
                        "http://taxprosuite.com")
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
