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
                        .WithOrigins("http://localhost:4200")
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
