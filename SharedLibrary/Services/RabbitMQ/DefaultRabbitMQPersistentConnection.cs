using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;

namespace SharedLibrary.Services.RabbitMQ;

public sealed class DefaultRabbitMQPersistentConnection(
        IConnectionFactory factory,
        ILogger<DefaultRabbitMQPersistentConnection> logger,
        IOptions<RabbitMQOptions> options) : IRabbitMQPersistentConnection
{
  private readonly object _syncRoot = new();
  private IConnection? _connection;
  private readonly int _retryCount = options.Value.RetryCount;

  public bool IsConnected =>
      _connection is { IsOpen: true };

  public IModel CreateModel()
  {
    if (!IsConnected) throw new InvalidOperationException("Services Broker no disponible");
    return _connection!.CreateModel();
  }

  public bool TryConnect()
  {
    logger.LogInformation("Services Broker: intentando conectar…");

    lock (_syncRoot)
    {
      var policy = Policy
          .Handle<SocketException>()
          .Or<BrokerUnreachableException>()
          .WaitAndRetry(_retryCount,
                        retry => TimeSpan.FromSeconds(Math.Pow(2, retry)),
                        (ex, ts) => logger.LogWarning(ex, "Reintentando en {Delay}s", ts.TotalSeconds));

      policy.Execute(() => _connection = factory.CreateConnection());

      if (IsConnected)
      {
        _connection!.ConnectionShutdown += (_, _) => TryConnect();
        logger.LogInformation("Services Broker: conexión establecida a {Host}", options.Value.HostName);
        return true;
      }

      logger.LogCritical("RabbitMQ: NO se pudo conectar.");
      return false;
    }
  }

  public void Dispose() => _connection?.Dispose();
}

public sealed class RabbitMQOptions
{
  public string HostName { get; init; } = "rabbitmq";
  public int Port { get; init; } = 5672;
  public string UserName { get; init; } = "guest";
  public string Password { get; init; } = "guest";
  public int RetryCount { get; init; } = 5;
  public string ExchangeName { get; init; } = "EventBusExchange";
}
