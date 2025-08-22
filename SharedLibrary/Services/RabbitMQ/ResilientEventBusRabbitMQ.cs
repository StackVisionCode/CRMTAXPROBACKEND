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

public sealed class ResilientEventBusRabbitMQ : IEventBus, IDisposable
{
    private readonly IRabbitMQPersistentConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ResilientEventBusRabbitMQ> _logger;
    private readonly InMemoryEventBusSubscriptionsManager _subsManager;
    private readonly RabbitMQConnectionState _connectionState;
    private readonly string _exchange;
    private readonly string _queue;
    private readonly Dictionary<string, IModel> _consumerChannels = new();
    private readonly object _channelLock = new();
    private bool _disposed = false;

    public ResilientEventBusRabbitMQ(
        IRabbitMQPersistentConnection connection,
        IServiceProvider serviceProvider,
        IOptions<RabbitMQOptions> settings,
        ILogger<ResilientEventBusRabbitMQ> logger,
        InMemoryEventBusSubscriptionsManager subsManager,
        RabbitMQConnectionState connectionState
    )
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _subsManager = subsManager;
        _connectionState = connectionState;
        _exchange = settings.Value.ExchangeName;
        _queue = $"{AppDomain.CurrentDomain.FriendlyName}.Queue";

        // Suscribirse al evento de reconexión
        _connection.OnReconnected += HandleReconnection;
    }

    public void Publish(IntegrationEvent @event)
    {
        if (_disposed)
        {
            _logger.LogWarning("❌ Attempt to publish on disposed EventBus");
            return;
        }

        var eventName = @event.GetType().Name;

        // CRÍTICO: Si no hay conexión, encolar el evento
        if (!_connection.IsConnected)
        {
            _logger.LogWarning(
                "⚠️ RabbitMQ no disponible - encolando evento {EventName} (ID: {EventId}) para envío posterior",
                eventName,
                @event.Id
            );

            _connectionState.EnqueuePendingEvent(@event, eventName);
            _logger.LogDebug("📋 Eventos pendientes: {Count}", _connectionState.PendingEventsCount);
            return;
        }

        // Publicar inmediatamente si hay conexión
        PublishInternal(@event);
    }

    private void PublishInternal(IntegrationEvent @event)
    {
        var eventName = @event.GetType().Name;

        try
        {
            using var channel = _connection.CreateModel();

            EnsureExchangeQueue(channel);

            var message = JsonConvert.SerializeObject(
                @event,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    NullValueHandling = NullValueHandling.Ignore,
                }
            );

            var body = Encoding.UTF8.GetBytes(message);
            var properties = channel.CreateBasicProperties();

            properties.DeliveryMode = 2; // Persistente
            properties.MessageId = @event.Id.ToString();
            properties.Timestamp = new AmqpTimestamp(
                ((DateTimeOffset)@event.OccurredOn).ToUnixTimeSeconds()
            );
            properties.ContentType = "application/json";

            channel.BasicPublish(
                exchange: _exchange,
                routingKey: eventName,
                basicProperties: properties,
                body: body
            );

            _logger.LogInformation(
                "📤 Evento {EventName} publicado exitosamente (ID: {EventId}, {Bytes} bytes)",
                eventName,
                @event.Id,
                body.Length
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Error publicando evento {EventName} (ID: {EventId})",
                eventName,
                @event.Id
            );

            // CRÍTICO: Si falla el envío, encolar para reintento
            _connectionState.EnqueuePendingEvent(@event, eventName);
            throw;
        }
    }

    public void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        if (_disposed)
        {
            _logger.LogWarning("❌ Attempt to subscribe on disposed EventBus");
            return;
        }

        var eventName = typeof(TEvent).Name;

        lock (_channelLock)
        {
            _subsManager.AddSubscription<TEvent, THandler>();
            _logger.LogInformation(
                "🔧 Suscripción registrada: {EventName} -> {Handler}",
                eventName,
                typeof(THandler).Name
            );

            if (_consumerChannels.ContainsKey(eventName))
            {
                _logger.LogDebug("ℹ️ Consumer ya existe para {EventName}", eventName);
                return;
            }

            if (_connection.IsConnected)
            {
                CreateConsumerForEvent(eventName);
            }
            else
            {
                _logger.LogInformation(
                    "📋 RabbitMQ no conectado - consumer para {EventName} se creará al conectar",
                    eventName
                );

                // Monitoreo en background para crear consumer cuando se conecte
                _ = Task.Run(async () =>
                {
                    var maxWait = TimeSpan.FromMinutes(5);
                    var startTime = DateTime.UtcNow;

                    while (!_disposed && DateTime.UtcNow - startTime < maxWait)
                    {
                        if (_connection.IsConnected)
                        {
                            lock (_channelLock)
                            {
                                if (!_consumerChannels.ContainsKey(eventName))
                                {
                                    CreateConsumerForEvent(eventName);
                                    _logger.LogInformation(
                                        "Consumer creado para {EventName} después de espera",
                                        eventName
                                    );
                                }
                            }
                            break;
                        }
                        await Task.Delay(5000);
                    }
                });
            }
        }
    }

    private void CreateConsumerForEvent(string eventName)
    {
        try
        {
            if (_consumerChannels.ContainsKey(eventName) || !_connection.IsConnected)
            {
                return;
            }

            var channel = _connection.CreateModel();
            _consumerChannels[eventName] = channel;

            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
            EnsureExchangeQueue(channel);
            channel.QueueBind(_queue, _exchange, eventName);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (sender, ea) => await ProcessEventGeneric(channel, ea);

            channel.BasicConsume(queue: _queue, autoAck: false, consumer: consumer);

            _logger.LogInformation("🎯 Consumer ACTIVO para {EventName}", eventName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creando consumer para {EventName}", eventName);

            if (_consumerChannels.TryGetValue(eventName, out var errorChannel))
            {
                try
                {
                    errorChannel?.Dispose();
                }
                catch { }
                _consumerChannels.Remove(eventName);
            }
        }
    }

    private void HandleReconnection()
    {
        _logger.LogInformation("🔄 Manejando reconexión de RabbitMQ...");

        // 1. Limpiar canales antiguos
        lock (_channelLock)
        {
            foreach (var kvp in _consumerChannels.ToList())
            {
                try
                {
                    kvp.Value?.Dispose();
                }
                catch { }
            }
            _consumerChannels.Clear();

            // 2. Recrear consumers
            var subscribedEvents = _subsManager.GetAllSubscribedEvents().ToList();
            foreach (var eventName in subscribedEvents)
            {
                try
                {
                    CreateConsumerForEvent(eventName);
                    _logger.LogInformation("Consumer reestablecido para {EventName}", eventName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "❌ Error reestableciendo consumer para {EventName}",
                        eventName
                    );
                }
            }
        }

        // 3. CRÍTICO: Procesar eventos pendientes
        _ = Task.Run(async () =>
        {
            await Task.Delay(2000); // Dar tiempo a que se establezcan los consumers
            await ProcessPendingEvents();
        });

        _logger.LogInformation("Reconexión completada");
    }

    private async Task ProcessPendingEvents()
    {
        try
        {
            var pendingEvents = _connectionState.GetAndClearPendingEvents().ToList();

            if (pendingEvents.Any())
            {
                _logger.LogInformation(
                    "📤 Procesando {Count} eventos pendientes...",
                    pendingEvents.Count
                );

                foreach (var pendingEvent in pendingEvents)
                {
                    try
                    {
                        if (pendingEvent.Event is IntegrationEvent integrationEvent)
                        {
                            PublishInternal(integrationEvent);
                            _logger.LogDebug(
                                "Evento pendiente {EventName} procesado",
                                pendingEvent.EventName
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "❌ Error procesando evento pendiente {EventName}",
                            pendingEvent.EventName
                        );
                    }

                    await Task.Delay(100); // Pequeño delay entre eventos
                }

                _logger.LogInformation("Procesamiento de eventos pendientes completado");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error procesando eventos pendientes");
        }
    }

    private async Task ProcessEventGeneric(IModel channel, BasicDeliverEventArgs ea)
    {
        var eventName = ea.RoutingKey;
        var messageId = ea.BasicProperties?.MessageId ?? "unknown";

        try
        {
            _logger.LogDebug(
                "📨 Procesando evento {EventName} (MessageId: {MessageId})",
                eventName,
                messageId
            );

            var raw = Encoding.UTF8.GetString(ea.Body.ToArray());
            if (string.IsNullOrWhiteSpace(raw))
            {
                _logger.LogWarning("⚠️ Mensaje vacío para {EventName}", eventName);
                channel.BasicNack(ea.DeliveryTag, false, false);
                return;
            }

            var handlerTypes = _subsManager.GetHandlersForEvent(eventName).ToList();
            if (!handlerTypes.Any())
            {
                _logger.LogDebug("ℹ️ No handlers para {EventName}", eventName);
                channel.BasicAck(ea.DeliveryTag, false);
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var allHandlersSucceeded = true;

            foreach (var handlerType in handlerTypes)
            {
                try
                {
                    var handler = scope.ServiceProvider.GetRequiredService(handlerType);
                    var method = handlerType.GetMethod("Handle");

                    if (method == null)
                    {
                        _logger.LogError(
                            "❌ Método Handle no encontrado en {HandlerType}",
                            handlerType.Name
                        );
                        continue;
                    }

                    var eventObj = DeserializeEventByName(raw, eventName);
                    if (eventObj == null)
                    {
                        _logger.LogWarning("⚠️ Error deserializando {EventName}", eventName);
                        continue;
                    }

                    var task = (Task)method.Invoke(handler, new object[] { eventObj })!;
                    await task;

                    _logger.LogDebug(
                        "Handler {HandlerType} procesó {EventName}",
                        handlerType.Name,
                        eventName
                    );
                }
                catch (Exception handlerEx)
                {
                    _logger.LogError(
                        handlerEx,
                        "❌ Error en handler {HandlerType} para {EventName}",
                        handlerType.Name,
                        eventName
                    );
                    allHandlersSucceeded = false;
                }
            }

            if (allHandlersSucceeded)
            {
                channel.BasicAck(ea.DeliveryTag, false);
                _logger.LogDebug("Evento {EventName} procesado exitosamente", eventName);
            }
            else
            {
                channel.BasicNack(ea.DeliveryTag, false, true);
                _logger.LogWarning(
                    "⚠️ Evento {EventName} requeue por errores en handlers",
                    eventName
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error inesperado procesando {EventName}", eventName);
            channel.BasicNack(ea.DeliveryTag, false, true);
        }
    }

    private object? DeserializeEventByName(string json, string eventName)
    {
        try
        {
            var type =
                _subsManager.GetEventTypeByName(eventName)
                ?? AppDomain
                    .CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == eventName);

            if (type == null)
            {
                _logger.LogWarning("⚠️ Tipo no encontrado para {EventName}", eventName);
                return null;
            }

            return JsonConvert.DeserializeObject(json, type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deserializando {EventName}", eventName);
            return null;
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
                        _logger.LogInformation("Channel eliminado para {EventName}", eventName);
                    }
                    else
                    {
                        _logger.LogInformation("ℹ️ Handler eliminado de {EventName}", eventName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error en unsubscribe de {EventName}", eventName);
                }
            }
        }
    }

    private void EnsureExchangeQueue(IModel channel)
    {
        channel.ExchangeDeclare(_exchange, ExchangeType.Direct, durable: true);
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
        _connection.OnReconnected -= HandleReconnection;

        lock (_channelLock)
        {
            foreach (var channel in _consumerChannels.Values)
            {
                try
                {
                    channel?.Dispose();
                }
                catch { }
            }
            _consumerChannels.Clear();
        }
    }
}
