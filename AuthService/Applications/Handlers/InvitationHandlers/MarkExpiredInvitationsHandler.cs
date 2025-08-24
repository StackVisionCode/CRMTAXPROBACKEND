using AuthService.Commands.InvitationCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.InvitationEvents;

namespace Handlers.InvitationHandlers;

/// <summary>
/// Handler para marcar invitaciones expiradas (job en background)
/// </summary>
public class MarkExpiredInvitationsHandler
    : IRequestHandler<MarkExpiredInvitationsCommand, ApiResponse<int>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<MarkExpiredInvitationsHandler> _logger;
    private readonly IEventBus _eventBus;

    public MarkExpiredInvitationsHandler(
        ApplicationDbContext dbContext,
        ILogger<MarkExpiredInvitationsHandler> logger,
        IEventBus eventBus
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<ApiResponse<int>> Handle(
        MarkExpiredInvitationsCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var now = DateTime.UtcNow;

            // 1. Obtener invitaciones que han expirado
            var expiredInvitationsQuery =
                from i in _dbContext.Invitations
                join c in _dbContext.Companies on i.CompanyId equals c.Id
                where i.Status == InvitationStatus.Pending && i.ExpiresAt <= now
                select new
                {
                    i.Id,
                    i.CompanyId,
                    i.Email,
                    i.Token,
                    CompanyName = c.CompanyName,
                    CompanyDomain = c.Domain,
                };

            var expiredInvitations = await expiredInvitationsQuery.ToListAsync(cancellationToken);

            if (!expiredInvitations.Any())
            {
                _logger.LogDebug("No expired invitations found");
                return new ApiResponse<int>(true, "No expired invitations found", 0);
            }

            // 2. Actualizar estado a expirado
            var expiredIds = expiredInvitations.Select(e => e.Id).ToList();
            var rowsAffected = await _dbContext
                .Invitations.Where(i => i.Status == InvitationStatus.Pending && i.ExpiresAt <= now)
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(i => i.Status, InvitationStatus.Expired)
                            .SetProperty(i => i.UpdatedAt, now),
                    cancellationToken
                );

            _logger.LogInformation("Marked {Count} invitations as expired", rowsAffected);

            // 3. Publicar eventos de expiración
            foreach (var expiredInvitation in expiredInvitations)
            {
                _eventBus.Publish(
                    new InvitationExpiredEvent(
                        Id: Guid.NewGuid(),
                        OccurredOn: DateTime.UtcNow,
                        InvitationId: expiredInvitation.Id,
                        CompanyId: expiredInvitation.CompanyId,
                        Email: expiredInvitation.Email,
                        Token: expiredInvitation.Token,
                        CompanyName: expiredInvitation.CompanyName,
                        CompanyDomain: expiredInvitation.CompanyDomain
                    )
                );
            }

            return new ApiResponse<int>(
                true,
                $"Marked {rowsAffected} invitations as expired",
                rowsAffected
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking expired invitations");
            return new ApiResponse<int>(false, ex.Message, 0);
        }
    }
}
