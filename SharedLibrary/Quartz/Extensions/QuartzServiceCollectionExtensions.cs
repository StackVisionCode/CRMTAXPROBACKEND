using Microsoft.Extensions.DependencyInjection;
using Quartz;
using SharedLibrary.Quartz.Schedulers;

namespace SharedLibrary.Extensions;

public static class QuartzServiceCollectionExtensions
{
    [Obsolete]
    public static IServiceCollection AddQuartzShared(this IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        services.AddSingleton<QuartzScheduler>();

        return services;
    }
}
