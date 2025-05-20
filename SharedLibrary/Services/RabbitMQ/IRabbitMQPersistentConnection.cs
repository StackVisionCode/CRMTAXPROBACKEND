using RabbitMQ.Client;

namespace SharedLibrary.Services.RabbitMQ;

public interface IRabbitMQPersistentConnection : IDisposable
{
    bool IsConnected { get; }
    bool TryConnect();
    IModel CreateModel();
}
