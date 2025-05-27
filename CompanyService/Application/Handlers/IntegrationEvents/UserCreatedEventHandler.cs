using SharedLibrary.Contracts;
using SharedLibrary.DTOs;

namespace CompanyService.Application.Handlers.IntegrationEvents;

public sealed class UserCreatedEventHandler : IIntegrationEventHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger;
    
    public UserCreatedEventHandler(ILogger<UserCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(UserCreatedEvent @event)
    {
        try
        {
            _logger.LogInformation("CompanyService procesando UserCreatedEvent para Usuario: {UserId}", @event.UserId);
            
            // Tu lógica de negocio aquí
            await ProcessUserCreation(@event);
            
            _logger.LogInformation("UserCreatedEvent procesado exitosamente para Usuario: {UserId}", @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando UserCreatedEvent para Usuario: {UserId}", @event.UserId);
            throw; // Re-lanzar para que RabbitMQ maneje el requeue
        }
    }
    
    private async Task ProcessUserCreation(UserCreatedEvent userEvent)
    {
        // Implementa tu lógica específica aquí
        await Task.CompletedTask;
    }
}