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

    // conexión persistente
    services.AddSingleton<IRabbitMQPersistentConnection>(sp =>
    {
      var opt = sp.GetRequiredService<IOptions<RabbitMQOptions>>().Value;
      var factory = new ConnectionFactory
      {
        HostName = opt.HostName,
        Port = opt.Port,
        UserName = opt.UserName,
        Password = opt.Password,
        DispatchConsumersAsync = true
      };
      var logger = sp.GetRequiredService<ILogger<DefaultRabbitMQPersistentConnection>>();
      var conn = new DefaultRabbitMQPersistentConnection(factory, logger, sp.GetRequiredService<IOptions<RabbitMQOptions>>());
      conn.TryConnect();
      return conn;
    });

    services.AddSingleton<InMemoryEventBusSubscriptionsManager>();
    services.AddSingleton<IEventBus, EventBusRabbitMQ>();

    return services;
  }
}
