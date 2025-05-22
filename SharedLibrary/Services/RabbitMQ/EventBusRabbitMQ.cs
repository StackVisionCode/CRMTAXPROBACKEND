using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;
using System.Text;

namespace SharedLibrary.Services.RabbitMQ;

public sealed class EventBusRabbitMQ(
        IRabbitMQPersistentConnection connection,
        IServiceProvider serviceProvider,
        IOptions<RabbitMQOptions> settings,
        ILogger<EventBusRabbitMQ> logger,
        InMemoryEventBusSubscriptionsManager subsManager)
    : IEventBus
{
  private readonly string _exchange = settings.Value.ExchangeName;
  private readonly string _queue = $"{AppDomain.CurrentDomain.FriendlyName}.Queue";
  private readonly IModel _channel = connection.CreateModel();

  public void Publish(IntegrationEvent @event)
  {
    EnsureExchangeQueue();
    var eventName = @event.GetType().Name;

    var message = JsonConvert.SerializeObject(@event);
    var body = Encoding.UTF8.GetBytes(message);
    var properties = _channel.CreateBasicProperties();
    properties.DeliveryMode = 2; // persistente

    _channel.BasicPublish(_exchange, eventName, properties, body);
    logger.LogInformation("Evento {EventName} publicado en RabbitMQ", eventName);
  }

  public void Subscribe<TEvent, THandler>()
      where TEvent : IntegrationEvent
      where THandler : class, IIntegrationEventHandler<TEvent>
  {
    var eventName = typeof(TEvent).Name;

    subsManager.AddSubscription<TEvent, THandler>();
    EnsureExchangeQueue();
    _channel.QueueBind(_queue, _exchange, eventName);

    var consumer = new AsyncEventingBasicConsumer(_channel);
    consumer.Received += async (_, ea) =>
    {
      var raw = Encoding.UTF8.GetString(ea.Body.ToArray());
      var evtType = typeof(TEvent);

      if (ea.RoutingKey != evtType.Name) return;

      var evtObj = JsonConvert.DeserializeObject(raw, evtType)!;
      using var scope = serviceProvider.CreateScope();
      foreach (var handlerType in subsManager.GetHandlersForEvent(ea.RoutingKey))
      {
        var handler = scope.ServiceProvider.GetRequiredService(handlerType);
        var method = handlerType.GetMethod("Handle");
        await (Task)method!.Invoke(handler, [evtObj])!;
      }
      _channel.BasicAck(ea.DeliveryTag, false);
    };

    _channel.BasicConsume(_queue, autoAck: false, consumer);
    logger.LogInformation("Suscrito a {EventName}", eventName);
  }

  public void Unsubscribe<TEvent, THandler>()
      where TEvent : IntegrationEvent
      where THandler : class, IIntegrationEventHandler<TEvent>
  {
    var eventName = typeof(TEvent).Name;

    subsManager.RemoveSubscription<TEvent, THandler>();

    // Si ya no queda ningún handler para ese evento, des-enlazamos la cola
    if (!subsManager.HasSubscriptionsForEvent(eventName))
    {
      _channel.QueueUnbind(_queue, _exchange, eventName);
      logger.LogInformation("❌  Unbound {EventName} de la cola", eventName);
    }
    else
    {
      logger.LogInformation("🛈  Handler {Handler} eliminado de {EventName}",
                            typeof(THandler).Name, eventName);
    }
  }

  private void EnsureExchangeQueue()
  {
    _channel.ExchangeDeclare(_exchange, ExchangeType.Direct, durable: true);
    _channel.QueueDeclare(_queue, durable: true, exclusive: false, autoDelete: false);
  }
}