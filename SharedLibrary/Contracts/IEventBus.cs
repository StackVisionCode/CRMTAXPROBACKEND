using SharedLibrary.DTOs;

namespace SharedLibrary.Contracts;

public interface IEventBus
{
    void Publish(IntegrationEvent @event);

    void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>;

    void Unsubscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>;
}
