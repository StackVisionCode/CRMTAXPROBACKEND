using AuthService.Commands.InvitationCommands;
using MediatR;

namespace AuthService.BackgroundServices;

/// <summary>
/// Servicio en background para limpiar invitaciones expiradas automáticamente
/// Se ejecuta cada hora para marcar invitaciones pendientes como expiradas
/// </summary>
public class InvitationCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InvitationCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromDays(15); // Ejecutar cada 15 Dias

    public InvitationCleanupService(
        IServiceProvider serviceProvider,
        ILogger<InvitationCleanupService> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Invitation cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MarkExpiredInvitationsAsync(stoppingToken);
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in invitation cleanup service");

                // Wait before retrying in case of error
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("Invitation cleanup service stopped");
    }

    private async Task MarkExpiredInvitationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new MarkExpiredInvitationsCommand();
            var result = await mediator.Send(command, cancellationToken);

            if ((result.Success ?? false) && result.Data > 0)
            {
                _logger.LogInformation("Marked {Count} invitations as expired", result.Data);
            }
            else if (!(result.Success ?? true))
            {
                _logger.LogWarning("Failed to mark expired invitations: {Message}", result.Message);
            }
            // Si Data == 0, no hay invitaciones expiradas, no logear nada
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking expired invitations");
            throw;
        }
    }
}
