using SharedLibrary.DTOs;

namespace SharedLibrary.Contracts;

public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IntegrationEvent
{
    Task Handle(TEvent @event);
}
