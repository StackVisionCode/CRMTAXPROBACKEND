using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace SharedLibrary.Services.RabbitMQ;

public sealed class DefaultRabbitMQPersistentConnection : IRabbitMQPersistentConnection
{
    private readonly IConnectionFactory _factory;
    private readonly ILogger<DefaultRabbitMQPersistentConnection> _logger;
    private readonly RabbitMQOptions _options;
    private readonly object _syncRoot = new();
    private readonly Timer _reconnectionTimer;
    private IConnection? _connection;
    private bool _disposed = false;
    private bool _isReconnecting = false;

    public DefaultRabbitMQPersistentConnection(
        IConnectionFactory factory,
        ILogger<DefaultRabbitMQPersistentConnection> logger,
        IOptions<RabbitMQOptions> options
    )
    {
        _factory = factory;
        _logger = logger;
        _options = options.Value;

        // Timer para reconexión automática cada 10 segundos cuando no hay conexión
        _reconnectionTimer = new Timer(
            ReconnectionTimerCallback,
            null,
            Timeout.Infinite,
            Timeout.Infinite
        );
    }

    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    public IModel CreateModel()
    {
        if (!IsConnected)
        {
            _logger.LogError("Cannot create RabbitMQ model - no connection available");
            throw new InvalidOperationException("Services Broker no disponible para crear modelo");
        }

        var model = _connection!.CreateModel();

        // Configurar QoS para el canal
        model.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        return model;
    }

    public bool TryConnect()
    {
        if (_disposed)
        {
            _logger.LogWarning("Attempt to connect on disposed connection");
            return false;
        }

        // Si ya estamos conectados, no hacer nada
        if (IsConnected)
        {
            return true;
        }

        _logger.LogInformation(
            "Services Broker: intentando conectar a {HostName}:{Port}...",
            _options.HostName,
            _options.Port
        );

        lock (_syncRoot)
        {
            if (_disposed || IsConnected)
            {
                return IsConnected;
            }

            try
            {
                // CAMBIO IMPORTANTE: Intento directo sin retry policy para no bloquear el startup
                _connection?.Dispose();
                _connection = _factory.CreateConnection(
                    $"{AppDomain.CurrentDomain.FriendlyName}-Connection"
                );

                if (IsConnected)
                {
                    _connection!.ConnectionShutdown += OnConnectionShutdown;
                    _connection!.ConnectionBlocked += OnConnectionBlocked;
                    _connection!.ConnectionUnblocked += OnConnectionUnblocked;

                    // Detener el timer de reconexión si está corriendo
                    _reconnectionTimer.Change(Timeout.Infinite, Timeout.Infinite);

                    _logger.LogInformation(
                        "✅ Services Broker: conexión establecida a {HostName}:{Port}",
                        _options.HostName,
                        _options.Port
                    );

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "❌ RabbitMQ no disponible en {HostName}:{Port} - iniciando reconexión automática",
                    _options.HostName,
                    _options.Port
                );

                // Iniciar reconexión automática SIN bloquear el startup
                StartReconnectionTimer();
                return false;
            }

            _logger.LogWarning("RabbitMQ: conexión no establecida - reintentando en segundo plano");
            StartReconnectionTimer();
            return false;
        }
    }

    private void StartReconnectionTimer()
    {
        if (_disposed || _isReconnecting)
            return;

        // Iniciar timer para reconexión cada 10 segundos
        _reconnectionTimer.Change(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        _logger.LogInformation("Iniciando reconexión automática cada 10 segundos...");
    }

    private void ReconnectionTimerCallback(object? state)
    {
        if (_disposed || IsConnected || _isReconnecting)
        {
            return;
        }

        _isReconnecting = true;

        try
        {
            _logger.LogDebug("🔄 Intentando reconexión automática a RabbitMQ...");

            // Usar el retry policy solo en el timer de reconexión, no en el startup
            var policy = Policy
                .Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .Or<ConnectFailureException>()
                .WaitAndRetry(
                    3, // Solo 3 intentos rápidos en el timer
                    retry => TimeSpan.FromSeconds(2),
                    (ex, delay, retryCount, context) =>
                    {
                        _logger.LogDebug("Reconexión intento {RetryCount}/3", retryCount);
                    }
                );

            var connected = false;
            policy.Execute(() =>
            {
                if (!IsConnected)
                {
                    _connection?.Dispose();
                    _connection = _factory.CreateConnection(
                        $"{AppDomain.CurrentDomain.FriendlyName}-Connection"
                    );
                    connected = IsConnected;
                }
            });

            if (connected)
            {
                _connection!.ConnectionShutdown += OnConnectionShutdown;
                _connection!.ConnectionBlocked += OnConnectionBlocked;
                _connection!.ConnectionUnblocked += OnConnectionUnblocked;

                _logger.LogInformation("✅ Reconexión automática exitosa a RabbitMQ");

                // Detener el timer de reconexión
                _reconnectionTimer.Change(Timeout.Infinite, Timeout.Infinite);

                // Notificar reconexión
                OnReconnected?.Invoke();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "❌ Reconexión automática falló, reintentando en 10 segundos...");
        }
        finally
        {
            _isReconnecting = false;
        }
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs args)
    {
        if (_disposed)
            return;

        _logger.LogWarning("RabbitMQ connection shutdown: {Reason}", args.ReplyText);

        // Iniciar reconexión automática
        StartReconnectionTimer();
    }

    private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs args)
    {
        _logger.LogWarning("RabbitMQ connection blocked: {Reason}", args.Reason);
    }

    private void OnConnectionUnblocked(object? sender, EventArgs args)
    {
        _logger.LogInformation("RabbitMQ connection unblocked");
    }

    // Evento para notificar cuando se reestablece la conexión
    public event Action? OnReconnected;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Detener el timer de reconexión
        _reconnectionTimer?.Dispose();

        try
        {
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ connection");
        }
    }
}
