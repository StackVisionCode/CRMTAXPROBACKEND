using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace SharedLibrary.Services.RabbitMQ;

public sealed class EnhancedRabbitMQPersistentConnection : IRabbitMQPersistentConnection
{
    private readonly IConnectionFactory _factory;
    private readonly ILogger<EnhancedRabbitMQPersistentConnection> _logger;
    private readonly RabbitMQOptions _options;
    private readonly RabbitMQConnectionState _connectionState;
    private readonly object _connectionLock = new();
    private readonly Timer _healthCheckTimer;
    private readonly Timer _reconnectionTimer;

    private IConnection? _connection;
    private bool _disposed = false;

    public EnhancedRabbitMQPersistentConnection(
        IConnectionFactory factory,
        ILogger<EnhancedRabbitMQPersistentConnection> logger,
        IOptions<RabbitMQOptions> options,
        RabbitMQConnectionState connectionState
    )
    {
        _factory = factory;
        _logger = logger;
        _options = options.Value;
        _connectionState = connectionState;

        // Health check cada 30 segundos
        _healthCheckTimer = new Timer(
            HealthCheckCallback,
            null,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30)
        );

        // Reconexión cada 10 segundos cuando no hay conexión
        _reconnectionTimer = new Timer(
            ReconnectionCallback,
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
            throw new InvalidOperationException(
                "RabbitMQ no está conectado - no se puede crear modelo"
            );
        }

        var model = _connection!.CreateModel();
        model.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        return model;
    }

    public bool TryConnect()
    {
        if (_disposed)
        {
            return false;
        }

        if (IsConnected)
        {
            _connectionState.IsConnected = true;
            return true;
        }

        lock (_connectionLock)
        {
            if (_disposed || IsConnected)
            {
                return IsConnected;
            }

            if (_connectionState.IsConnecting)
            {
                return false; // Ya hay un intento en progreso
            }

            _connectionState.IsConnecting = true;

            try
            {
                _logger.LogDebug(
                    "🔄 Intentando conectar a RabbitMQ {Host}:{Port}...",
                    _options.HostName,
                    _options.Port
                );

                _connection?.Dispose();
                _connection = _factory.CreateConnection(
                    $"{AppDomain.CurrentDomain.FriendlyName}-Connection"
                );

                if (IsConnected)
                {
                    _connection!.ConnectionShutdown += OnConnectionShutdown;
                    _connection!.ConnectionBlocked += OnConnectionBlocked;
                    _connection!.ConnectionUnblocked += OnConnectionUnblocked;

                    _connectionState.IsConnected = true;
                    StopReconnectionTimer();

                    _logger.LogInformation(
                        "✅ RabbitMQ conectado exitosamente a {Host}:{Port}",
                        _options.HostName,
                        _options.Port
                    );

                    // Notificar reconexión
                    OnReconnected?.Invoke();
                    return true;
                }
            }
            catch (Exception ex)
                when (ex is BrokerUnreachableException or SocketException or ConnectFailureException
                )
            {
                _logger.LogDebug("❌ RabbitMQ no disponible: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "❌ Error inesperado conectando a RabbitMQ");
            }
            finally
            {
                _connectionState.IsConnecting = false;
            }

            _connectionState.IsConnected = false;
            StartReconnectionTimer();
            return false;
        }
    }

    private void HealthCheckCallback(object? state)
    {
        if (_disposed)
            return;

        try
        {
            var wasConnected = _connectionState.IsConnected;
            var isNowConnected = IsConnected;

            if (wasConnected != isNowConnected)
            {
                _connectionState.IsConnected = isNowConnected;

                if (!isNowConnected)
                {
                    _logger.LogWarning("⚠️ RabbitMQ conexión perdida detectada en health check");
                    StartReconnectionTimer();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error en health check de RabbitMQ");
        }
    }

    private void ReconnectionCallback(object? state)
    {
        if (_disposed || IsConnected)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var policy = Policy
                    .Handle<Exception>()
                    .WaitAndRetryAsync(
                        3,
                        retry => TimeSpan.FromSeconds(Math.Pow(2, retry)), // Backoff exponencial
                        (ex, delay, retryCount, context) =>
                        {
                            _logger.LogDebug(
                                "Intento de reconexión {RetryCount}/3 en {Delay}s",
                                retryCount,
                                delay.TotalSeconds
                            );
                        }
                    );

                await policy.ExecuteAsync(async () =>
                {
                    if (!IsConnected && TryConnect())
                    {
                        _logger.LogInformation("✅ Reconexión automática exitosa");
                    }
                    else if (!IsConnected)
                    {
                        throw new Exception("Reconexión falló");
                    }

                    await Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    ex,
                    "❌ Reconexión automática falló, reintentando en 10 segundos..."
                );
            }
        });
    }

    private void StartReconnectionTimer()
    {
        if (_disposed)
            return;

        _reconnectionTimer.Change(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }

    private void StopReconnectionTimer()
    {
        _reconnectionTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs args)
    {
        if (_disposed)
            return;

        _logger.LogWarning("⚠️ RabbitMQ connection shutdown: {Reason}", args.ReplyText);
        _connectionState.IsConnected = false;
        StartReconnectionTimer();
    }

    private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs args)
    {
        _logger.LogWarning("⚠️ RabbitMQ connection blocked: {Reason}", args.Reason);
    }

    private void OnConnectionUnblocked(object? sender, EventArgs args)
    {
        _logger.LogInformation("✅ RabbitMQ connection unblocked");
    }

    public event Action? OnReconnected;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _connectionState.IsConnected = false;

        _healthCheckTimer?.Dispose();
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
