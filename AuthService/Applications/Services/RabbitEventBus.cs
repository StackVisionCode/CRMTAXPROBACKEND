using System.Text;
using System.Text.Json;
using AuthService.Infraestructure.Services;
using RabbitMQ.Client;

namespace AuthService.Applications.Services;

public sealed class RabbitEventBus : IRabbitEventBus, IDisposable
{
    private readonly IConnection _conn;
    private readonly IModel      _ch;

    public RabbitEventBus(IConfiguration cfg)
    {
        var factory = new ConnectionFactory
        {
            HostName = cfg["Rabbit:Host"] ?? "rabbitmq",
            UserName = cfg["Rabbit:User"] ?? "guest",
            Password = cfg["Rabbit:Pass"] ?? "guest",
            DispatchConsumersAsync = true
        };
        _conn = factory.CreateConnection();
        _ch   = _conn.CreateModel();
    }

    public void Publish<T>(string exchange, string routingKey, T message)
    {
        _ch.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = _ch.CreateBasicProperties();
        props.Persistent = true;

        _ch.BasicPublish(exchange, routingKey, props, body);
    }

    public void Dispose()
    {
        _ch?.Dispose();
        _conn?.Dispose();
    }
}