using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;

namespace SharedLibrary.Services.RabbitMQ;

public sealed class DefaultRabbitMQPersistentConnection : IRabbitMQPersistentConnection
{
    private readonly IConnectionFactory _factory;
    private readonly ILogger<DefaultRabbitMQPersistentConnection> _logger;
    private readonly RabbitMQOptions _options;
    private readonly object _syncRoot = new();
    private IConnection? _connection;
    private bool _disposed = false;

    public DefaultRabbitMQPersistentConnection(
        IConnectionFactory factory,
        ILogger<DefaultRabbitMQPersistentConnection> logger,
        IOptions<RabbitMQOptions> options)
    {
        _factory = factory;
        _logger = logger;
        _options = options.Value;
    }

    public bool IsConnected =>
        _connection is { IsOpen: true } && !_disposed;

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
        _logger.LogInformation("Services Broker: intentando conectar a {HostName}:{Port}...", 
            _options.HostName, _options.Port);

        lock (_syncRoot)
        {
            if (_disposed)
            {
                _logger.LogWarning("Attempt to connect on disposed connection");
                return false;
            }

            var policy = Policy
                .Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .Or<ConnectFailureException>()
                .WaitAndRetry(
                    _options.RetryCount,
                    retry => TimeSpan.FromSeconds(Math.Pow(2, retry)),
                    (ex, delay, retryCount, context) =>
                    {
                        _logger.LogWarning(ex, 
                            "RabbitMQ connection attempt {RetryCount}/{MaxRetries} failed. Reintentando en {Delay}s", 
                            retryCount, _options.RetryCount, delay.TotalSeconds);
                    });

            try
            {
                policy.Execute(() =>
                {
                    _connection?.Dispose();
                    _connection = _factory.CreateConnection($"{AppDomain.CurrentDomain.FriendlyName}-Connection");
                });

                if (IsConnected)
                {
                    _connection!.ConnectionShutdown += OnConnectionShutdown;
                    
                    _logger.LogInformation("Services Broker: conexión establecida a {HostName}:{Port}", 
                        _options.HostName, _options.Port);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "RabbitMQ: NO se pudo conectar después de {RetryCount} intentos", 
                    _options.RetryCount);
                return false;
            }

            _logger.LogCritical("RabbitMQ: NO se pudo conectar - conexión no disponible");
            return false;
        }
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs args)
    {
        if (_disposed) return;
        
        _logger.LogWarning("RabbitMQ connection shutdown: {Reason}", args.ReplyText);
        
        // Intentar reconectar en un hilo separado
        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            if (!_disposed)
            {
                TryConnect();
            }
        });
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        
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