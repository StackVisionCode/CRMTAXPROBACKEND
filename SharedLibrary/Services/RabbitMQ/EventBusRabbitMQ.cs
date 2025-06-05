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
            _logger.LogWarning("RabbitMQ connection not available for publishing");
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

    // **NEW: Helper method to deserialize events by name**
    private object? DeserializeEventByName(string json, string eventName)
    {
        try
        {
            return eventName switch
            {
                nameof(UserLoginEvent) => JsonConvert.DeserializeObject<UserLoginEvent>(json),
                nameof(PasswordResetLinkEvent) =>
                    JsonConvert.DeserializeObject<PasswordResetLinkEvent>(json),
                nameof(PasswordResetOtpEvent) =>
                    JsonConvert.DeserializeObject<PasswordResetOtpEvent>(json),
                nameof(PasswordChangedEvent) => JsonConvert.DeserializeObject<PasswordChangedEvent>(
                    json
                ),
                _ => null,
            };
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

        if (_consumerChannels.ContainsKey(eventName))
        {
            _logger.LogInformation("Already subscribed to event {EventName}", eventName);
            return;
        }

        if (!_connection.IsConnected)
        {
            _logger.LogError("Cannot subscribe - RabbitMQ connection not available");
            return;
        }

        _subsManager.AddSubscription<TEvent, THandler>();

        var channel = _connection.CreateModel();
        _consumerChannels[eventName] = channel;

        try
        {
            EnsureExchangeQueue(channel);
            channel.QueueBind(_queue, _exchange, eventName);

            // **FIXED: Create a generic consumer that can handle any event type**
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (sender, ea) =>
            {
                await ProcessEventGeneric(channel, ea);
            };

            channel.BasicConsume(queue: _queue, autoAck: false, consumer: consumer);

            _logger.LogInformation("Subscribed to {EventName}", eventName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to event {EventName}", eventName);

            if (_consumerChannels.TryGetValue(eventName, out var errorChannel))
            {
                errorChannel?.Dispose();
                _consumerChannels.Remove(eventName);
            }

            throw;
        }
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
                        continue; // Skip this handler but continue with others
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
            channel.BasicNack(ea.DeliveryTag, false, true);
        }
    }

    private async Task ProcessEvent<TEvent>(IModel channel, BasicDeliverEventArgs ea)
        where TEvent : IntegrationEvent
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

            // **FIXED: Remove the strict type checking that was causing the mismatch**
            // The original code was rejecting events when the routing key didn't match the generic type
            // Instead, we should handle the event based on the actual routing key

            var raw = Encoding.UTF8.GetString(ea.Body.ToArray());

            if (string.IsNullOrWhiteSpace(raw))
            {
                _logger.LogWarning("Empty message body for event {EventName}", eventName);
                channel.BasicNack(ea.DeliveryTag, false, false);
                return;
            }

            // **FIXED: Handle events based on routing key instead of generic type**
            var handlerTypes = _subsManager.GetHandlersForEvent(eventName).ToList();
            if (!handlerTypes.Any())
            {
                // This is normal - not all services handle all events
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

                    // **FIXED: Deserialize to the correct event type based on routing key**
                    object? eventObj = DeserializeEventByName(raw, eventName);
                    if (eventObj == null)
                    {
                        _logger.LogWarning("Failed to deserialize event {EventName}", eventName);
                        channel.BasicNack(ea.DeliveryTag, false, false);
                        return;
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

                    channel.BasicNack(ea.DeliveryTag, false, true);
                    return;
                }
            }

            channel.BasicAck(ea.DeliveryTag, false);
            _logger.LogDebug(
                "Event {EventName} with MessageId {MessageId} processed successfully",
                eventName,
                messageId
            );
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(
                jsonEx,
                "JSON deserialization error for event {EventName} with MessageId {MessageId}",
                eventName,
                messageId
            );
            channel.BasicNack(ea.DeliveryTag, false, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error processing event {EventName} with MessageId {MessageId}",
                eventName,
                messageId
            );
            channel.BasicNack(ea.DeliveryTag, false, true);
        }
    }

    public void Unsubscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        var eventName = typeof(TEvent).Name;

        _subsManager.RemoveSubscription<TEvent, THandler>();

        if (_consumerChannels.TryGetValue(eventName, out var channel))
        {
            try
            {
                if (!_subsManager.HasSubscriptionsForEvent(eventName))
                {
                    channel.QueueUnbind(_queue, _exchange, eventName);
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

    private void EnsureExchangeQueue(IModel channel)
    {
        // Exchange durable
        channel.ExchangeDeclare(_exchange, ExchangeType.Direct, durable: true);

        // Queue durable y sin auto-delete
        channel.QueueDeclare(
            queue: _queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

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
