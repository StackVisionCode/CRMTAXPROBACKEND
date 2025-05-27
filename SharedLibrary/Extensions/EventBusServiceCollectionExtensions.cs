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
    public static IServiceCollection AddEventBus(this IServiceCollection services, IConfiguration cfg)
    {
        services.Configure<RabbitMQOptions>(cfg.GetSection("RabbitMQ"));

        // Conexión persistente mejorada
        services.AddSingleton<IRabbitMQPersistentConnection>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RabbitMQOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<DefaultRabbitMQPersistentConnection>>();
            
            var factory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password,
                VirtualHost = options.VirtualHost,
                
                // Configuraciones adicionales para mejorar la estabilidad
                RequestedHeartbeat = TimeSpan.FromSeconds(options.RequestedHeartbeat),
                NetworkRecoveryInterval = TimeSpan.FromSeconds(options.NetworkRecoveryInterval),
                AutomaticRecoveryEnabled = options.AutomaticRecoveryEnabled,
                TopologyRecoveryEnabled = options.TopologyRecoveryEnabled,
                ContinuationTimeout = TimeSpan.FromMilliseconds(options.RequestedConnectionTimeout),
                
                // Importante: habilitar consumo asíncrono
                DispatchConsumersAsync = true,
                
                // Configuración de cliente
                ClientProvidedName = $"{AppDomain.CurrentDomain.FriendlyName}-{Environment.MachineName}"
            };

            var connection = new DefaultRabbitMQPersistentConnection(
                factory, 
                logger, 
                sp.GetRequiredService<IOptions<RabbitMQOptions>>());
                
            // Intentar conectar al inicio
            if (!connection.TryConnect())
            {
                logger.LogWarning("No se pudo establecer conexión inicial con RabbitMQ");
            }
            
            return connection;
        });

        services.AddSingleton<InMemoryEventBusSubscriptionsManager>();
        services.AddSingleton<IEventBus, EventBusRabbitMQ>();

        return services;
    }
}