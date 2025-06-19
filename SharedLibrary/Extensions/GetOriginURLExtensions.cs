using Microsoft.Extensions.DependencyInjection;
using SharedLibrary.Helpers;

namespace SharedLibrary.Extensions;

public static class GetOriginURLExtensions
{
    public static IServiceCollection AddCustomOrigin(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<GetOriginURL>();

        return services;
    }
}
