using RabbitMQ.Client;

namespace SharedLibrary.Services.RabbitMQ;

public interface IRabbitMQPersistentConnection : IDisposable
{
    bool IsConnected { get; }
    bool TryConnect();
    IModel CreateModel();

    /// <summary>
    /// Evento que se dispara cuando se reestablece la conexión con RabbitMQ
    /// </summary>
    event Action? OnReconnected;
}
