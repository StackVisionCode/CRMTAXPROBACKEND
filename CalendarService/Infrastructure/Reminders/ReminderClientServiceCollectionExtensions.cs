// Infrastructure/Reminders/ReminderClientServiceCollectionExtensions.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Reminders;

public static class ReminderClientServiceCollectionExtensions
{
    public static IServiceCollection AddReminderClient(this IServiceCollection services, IConfiguration cfg)
    {
        services.Configure<ReminderClientOptions>(cfg.GetSection("ReminderClient"));
        services.AddHttpClient<IReminderClient, ReminderClient>(client =>
        {
            var baseUrl = cfg["ReminderClient:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
                client.BaseAddress = new Uri(baseUrl);
        });
        return services;
    }
}
