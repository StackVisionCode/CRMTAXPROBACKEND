using SharedLibrary.Contracts;
using SharedLibrary.DTOs;

namespace CompanyService.Application.Handlers.IntegrationEvents;

public sealed class UserCreatedEventHandler(ILogger<UserCreatedEventHandler> log)
    : IIntegrationEventHandler<UserCreatedEvent>
{
  public async Task Handle(UserCreatedEvent @event)
  {
    log.LogInformation("CompanyService recibió UserCreatedEvent: {UserId}", @event.UserId);
    // Ejemplo: actualizar tabla local, crear relación, etc.
    await Task.CompletedTask;
  }
}
