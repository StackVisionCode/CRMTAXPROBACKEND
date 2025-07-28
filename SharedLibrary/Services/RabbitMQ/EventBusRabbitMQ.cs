using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;

namespace SharedLibrary.Services.RabbitMQ;

public sealed class EventBusRabbitMQ : IEventBus, IDisposable
{
    private readonly IRabbitMQPersistentConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventBusRabbitMQ> _logger;
    private readonly InMemoryEventBusSubscriptionsManager _subsManager;
    private readonly string _exchange;
    private readonly string _queue;
    private readonly Dictionary<string, IModel> _consumerChannels = new();
    private readonly object _channelLock = new();
    private bool _disposed = false;

    public EventBusRabbitMQ(
        IRabbitMQPersistentConnection connection,
        IServiceProvider serviceProvider,
        IOptions<RabbitMQOptions> settings,
        ILogger<EventBusRabbitMQ> logger,
        InMemoryEventBusSubscriptionsManager subsManager
    )
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _subsManager = subsManager;
        _exchange = settings.Value.ExchangeName;
        _queue = $"{AppDomain.CurrentDomain.FriendlyName}.Queue";

        // Suscribirse al evento de reconexión para reestablecer consumidores
        _connection.OnReconnected += HandleReconnection;
    }

    public void Publish(IntegrationEvent @event)
    {
        if (_disposed)
        {
            _logger.LogWarning("Attempt to publish on disposed EventBus");
            return;
        }

        if (!_connection.IsConnected)
        {
            _logger.LogWarning(
                "RabbitMQ connection not available for publishing event {EventName}",
                @event.GetType().Name
            );
            return;
        }

        var eventName = @event.GetType().Name;

        using var channel = _connection.CreateModel();

        try
        {
            EnsureExchangeQueue(channel);

            var message = JsonConvert.SerializeObject(@event);
            var body = Encoding.UTF8.GetBytes(message);
            var properties = channel.CreateBasicProperties();

            // IMPORTANTE: Hacer los mensajes persistentes para que sobrevivan reinicios
            properties.DeliveryMode = 2; // persistente
            properties.MessageId = @event.Id.ToString();
            properties.Timestamp = new AmqpTimestamp(
                ((DateTimeOffset)@event.OccurredOn).ToUnixTimeSeconds()
            );

            channel.BasicPublish(
                exchange: _exchange,
                routingKey: eventName,
                basicProperties: properties,
                body: body
            );

            _logger.LogInformation(
                "Evento {EventName} con ID {EventId} publicado en RabbitMQ",
                eventName,
                @event.Id
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error publishing event {EventName} with ID {EventId}",
                eventName,
                @event.Id
            );
            throw;
        }
    }

    private object? DeserializeEventByName(string json, string eventName)
    {
        try
        {
            var type = _subsManager.GetEventTypeByName(eventName);

            type ??= AppDomain
                .CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == eventName);

            if (type is null)
            {
                _logger.LogWarning("No se encontró tipo CLR para {EventName}", eventName);
                return null;
            }

            return JsonConvert.DeserializeObject(json, type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing event {EventName}", eventName);
            return null;
        }
    }

    public void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        if (_disposed)
        {
            _logger.LogWarning("Attempt to subscribe on disposed EventBus");
            return;
        }

        var eventName = typeof(TEvent).Name;

        lock (_channelLock)
        {
            // CRÍTICO: Siempre registrar la suscripción primero
            _subsManager.AddSubscription<TEvent, THandler>();

            if (_consumerChannels.ContainsKey(eventName))
            {
                _logger.LogInformation("Already subscribed to event {EventName}", eventName);
                return;
            }

            // CAMBIO IMPORTANTE: Intentar crear consumidor inmediatamente, sin importar el estado de conexión
            // Esto asegura que se procesen mensajes en cola cuando el microservicio se levanta
            if (_connection.IsConnected)
            {
                CreateConsumerForEvent(eventName);
            }
            else
            {
                _logger.LogInformation(
                    "RabbitMQ no conectado - suscripción {EventName} registrada para cuando se conecte",
                    eventName
                );
                // CRÍTICO: Aún así intentar establecer la suscripción para procesar mensajes en cola
                _ = Task.Run(async () =>
                {
                    // Reintentar crear el consumidor cada 5 segundos hasta que se conecte
                    while (!_disposed && !_connection.IsConnected)
                    {
                        await Task.Delay(5000);
                        if (_connection.IsConnected && !_consumerChannels.ContainsKey(eventName))
                        {
                            lock (_channelLock)
                            {
                                if (!_consumerChannels.ContainsKey(eventName))
                                {
                                    CreateConsumerForEvent(eventName);
                                }
                            }
                            break;
                        }
                    }
                });
            }
        }

        _logger.LogInformation("✅ Suscrito a evento {EventName}", eventName);
    }

    private void CreateConsumerForEvent(string eventName)
    {
        try
        {
            if (_consumerChannels.ContainsKey(eventName))
            {
                return; // Ya existe
            }

            var channel = _connection.CreateModel();
            _consumerChannels[eventName] = channel;

            EnsureExchangeQueue(channel);
            channel.QueueBind(_queue, _exchange, eventName);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (sender, ea) =>
            {
                await ProcessEventGeneric(channel, ea);
            };

            channel.BasicConsume(queue: _queue, autoAck: false, consumer: consumer);

            _logger.LogInformation("Consumer creado para evento {EventName}", eventName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating consumer for event {EventName}", eventName);

            if (_consumerChannels.TryGetValue(eventName, out var errorChannel))
            {
                errorChannel?.Dispose();
                _consumerChannels.Remove(eventName);
            }
        }
    }

    private void HandleReconnection()
    {
        _logger.LogInformation("Reestableciendo consumidores después de reconexión...");

        lock (_channelLock)
        {
            // Limpiar canales antiguos
            foreach (var channel in _consumerChannels.Values)
            {
                try
                {
                    channel?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing old channel during reconnection");
                }
            }
            _consumerChannels.Clear();

            // CRÍTICO: Recrear TODOS los consumidores para eventos suscritos
            // Esto incluye eventos que pueden tener mensajes en cola esperando
            var subscribedEvents = _subsManager.GetAllSubscribedEvents();
            foreach (var eventName in subscribedEvents)
            {
                try
                {
                    CreateConsumerForEvent(eventName);
                    _logger.LogInformation(
                        "✅ Consumidor reestablecido para {EventName}",
                        eventName
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "❌ Error reestablishing consumer for {EventName}",
                        eventName
                    );
                }
            }
        }

        _logger.LogInformation(
            "✅ Todos los consumidores han sido reestablecidos - procesando mensajes en cola..."
        );
    }

    private async Task ProcessEventGeneric(IModel channel, BasicDeliverEventArgs ea)
    {
        var eventName = ea.RoutingKey;
        var messageId = ea.BasicProperties?.MessageId ?? "unknown";

        try
        {
            _logger.LogDebug(
                "Processing event {EventName} with MessageId {MessageId}",
                eventName,
                messageId
            );

            var raw = Encoding.UTF8.GetString(ea.Body.ToArray());

            if (string.IsNullOrWhiteSpace(raw))
            {
                _logger.LogWarning("Empty message body for event {EventName}", eventName);
                channel.BasicNack(ea.DeliveryTag, false, false);
                return;
            }

            var handlerTypes = _subsManager.GetHandlersForEvent(eventName).ToList();
            if (!handlerTypes.Any())
            {
                _logger.LogDebug(
                    "No handlers found for event {EventName} - acknowledging",
                    eventName
                );
                channel.BasicAck(ea.DeliveryTag, false);
                return;
            }

            using var scope = _serviceProvider.CreateScope();

            foreach (var handlerType in handlerTypes)
            {
                try
                {
                    var handler = scope.ServiceProvider.GetRequiredService(handlerType);
                    var method = handlerType.GetMethod("Handle");

                    if (method == null)
                    {
                        _logger.LogError(
                            "Handle method not found on {HandlerType}",
                            handlerType.Name
                        );
                        continue;
                    }

                    object? eventObj = DeserializeEventByName(raw, eventName);
                    if (eventObj == null)
                    {
                        _logger.LogWarning("Failed to deserialize event {EventName}", eventName);
                        continue;
                    }

                    var task = (Task)method.Invoke(handler, new object[] { eventObj })!;
                    await task;

                    _logger.LogDebug(
                        "Handler {HandlerType} processed event {EventName} successfully",
                        handlerType.Name,
                        eventName
                    );
                }
                catch (Exception handlerEx)
                {
                    _logger.LogError(
                        handlerEx,
                        "Error in handler {HandlerType} processing event {EventName} with MessageId {MessageId}",
                        handlerType.Name,
                        eventName,
                        messageId
                    );
                    // Continue with other handlers even if one fails
                }
            }

            channel.BasicAck(ea.DeliveryTag, false);
            _logger.LogDebug(
                "Event {EventName} with MessageId {MessageId} processed successfully",
                eventName,
                messageId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error processing event {EventName} with MessageId {MessageId}",
                eventName,
                messageId
            );

            // IMPORTANTE: Requeue el mensaje para que se procese cuando se reestablezca la conexión
            channel.BasicNack(ea.DeliveryTag, false, true);
        }
    }

    public void Unsubscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        var eventName = typeof(TEvent).Name;

        lock (_channelLock)
        {
            _subsManager.RemoveSubscription<TEvent, THandler>();

            if (_consumerChannels.TryGetValue(eventName, out var channel))
            {
                try
                {
                    if (!_subsManager.HasSubscriptionsForEvent(eventName))
                    {
                        if (_connection.IsConnected)
                        {
                            channel.QueueUnbind(_queue, _exchange, eventName);
                        }
                        channel.Dispose();
                        _consumerChannels.Remove(eventName);
                        _logger.LogInformation(
                            "Unbound and disposed channel for {EventName}",
                            eventName
                        );
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Handler {Handler} eliminado de {EventName}",
                            typeof(THandler).Name,
                            eventName
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error unsubscribing from event {EventName}", eventName);
                }
            }
        }
    }

    private void EnsureExchangeQueue(IModel channel)
    {
        // Exchange durable - IMPORTANTE para persistencia
        channel.ExchangeDeclare(_exchange, ExchangeType.Direct, durable: true);

        // Queue durable y sin auto-delete - IMPORTANTE para persistencia
        channel.QueueDeclare(
            queue: _queue,
            durable: true, // Sobrevive reinicios del broker
            exclusive: false,
            autoDelete: false, // No se elimina automáticamente
            arguments: null
        );
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Desuscribirse del evento de reconexión
        _connection.OnReconnected -= HandleReconnection;

        lock (_channelLock)
        {
            foreach (var channel in _consumerChannels.Values)
            {
                try
                {
                    channel?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing consumer channel");
                }
            }

            _consumerChannels.Clear();
        }
    }
}
