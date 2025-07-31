using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SharedLibrary.Contracts;
using SharedLibrary.Services.RabbitMQ;

namespace SharedLibrary.Extensions;

public static class EventBusServiceCollectionExtensions
{
    public static IServiceCollection AddEventBus(
        this IServiceCollection services,
        IConfiguration cfg
    )
    {
        // Configuración
        services.Configure<RabbitMQOptions>(cfg.GetSection("RabbitMQ"));

        // Estado de conexión compartido
        services.AddSingleton<RabbitMQConnectionState>();

        // Factory de conexión
        services.AddSingleton<IConnectionFactory>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RabbitMQOptions>>().Value;

            return new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password,
                VirtualHost = options.VirtualHost,
                RequestedHeartbeat = TimeSpan.FromSeconds(options.RequestedHeartbeat),
                NetworkRecoveryInterval = TimeSpan.FromSeconds(options.NetworkRecoveryInterval),
                AutomaticRecoveryEnabled = options.AutomaticRecoveryEnabled,
                TopologyRecoveryEnabled = options.TopologyRecoveryEnabled,
                ContinuationTimeout = TimeSpan.FromMilliseconds(options.RequestedConnectionTimeout),
                DispatchConsumersAsync = true,
                ClientProvidedName =
                    $"{AppDomain.CurrentDomain.FriendlyName}-{Environment.MachineName}",
            };
        });

        // Conexión persistente mejorada
        services.AddSingleton<
            IRabbitMQPersistentConnection,
            EnhancedRabbitMQPersistentConnection
        >();

        // Gestor de suscripciones
        services.AddSingleton<InMemoryEventBusSubscriptionsManager>();

        // EventBus resiliente
        services.AddSingleton<IEventBus, ResilientEventBusRabbitMQ>();

        // Servicio de startup
        services.AddHostedService<RabbitMQStartupService>();

        return services;
    }
}
