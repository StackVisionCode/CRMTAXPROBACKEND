namespace AuthService.Infraestructure.Services;

public interface IRabbitEventBus
{
    void Publish<T>(string exchange, string routingKey, T message);
}