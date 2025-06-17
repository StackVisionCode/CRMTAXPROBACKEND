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
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().SetIsOriginAllowed(_=>true);
                }
            );
        });

        return services;
    }
}
