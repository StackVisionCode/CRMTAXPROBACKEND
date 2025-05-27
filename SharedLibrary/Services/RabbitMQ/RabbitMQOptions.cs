namespace SharedLibrary.Services.RabbitMQ;

public sealed class RabbitMQOptions
{
    public string HostName { get; init; } = "rabbitmq";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public int RetryCount { get; init; } = 5;
    public string ExchangeName { get; init; } = "EventBusExchange";
    
    // Nuevas opciones para mejorar la configuración
    public string VirtualHost { get; init; } = "/";
    public int RequestedHeartbeat { get; init; } = 60;
    public int NetworkRecoveryInterval { get; init; } = 5;
    public bool AutomaticRecoveryEnabled { get; init; } = true;
    public bool TopologyRecoveryEnabled { get; init; } = true;
    public int RequestedConnectionTimeout { get; init; } = 30000; // 30 segundos en milisegundos
}