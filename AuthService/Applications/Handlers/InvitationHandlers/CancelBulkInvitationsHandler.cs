using AuthService.Commands.InvitationCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.InvitationEvents;

namespace Handlers.InvitationHandlers;

/// <summary>
/// Handler para cancelar múltiples invitaciones
/// </summary>
public class CancelBulkInvitationsHandler
    : IRequestHandler<CancelBulkInvitationsCommand, ApiResponse<int>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CancelBulkInvitationsHandler> _logger;
    private readonly IEventBus _eventBus;

    public CancelBulkInvitationsHandler(
        ApplicationDbContext dbContext,
        ILogger<CancelBulkInvitationsHandler> logger,
        IEventBus eventBus
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<ApiResponse<int>> Handle(
        CancelBulkInvitationsCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!request.CancelRequest.InvitationIds.Any())
            {
                return new ApiResponse<int>(false, "No invitation IDs provided", 0);
            }

            // 1. Obtener invitaciones válidas que el usuario puede cancelar
            var validInvitationsQuery =
                from i in _dbContext.Invitations
                join c in _dbContext.Companies on i.CompanyId equals c.Id
                join u in _dbContext.TaxUsers
                    on new
                    {
                        CompanyId = i.CompanyId,
                        UserId = request.CancelledByUserId,
                    } equals new { CompanyId = u.CompanyId, UserId = u.Id }
                where
                    request.CancelRequest.InvitationIds.Contains(i.Id)
                    && i.Status == InvitationStatus.Pending
                    && u.IsActive
                select new
                {
                    i.Id,
                    i.CompanyId,
                    i.Email,
                    i.Token,
                    CompanyName = c.CompanyName,
                    CompanyDomain = c.Domain,
                };

            var validInvitations = await validInvitationsQuery.ToListAsync(cancellationToken);

            if (!validInvitations.Any())
            {
                _logger.LogWarning(
                    "No valid invitations found to cancel for user {CancelledByUserId}. Requested IDs: {InvitationIds}",
                    request.CancelledByUserId,
                    string.Join(", ", request.CancelRequest.InvitationIds)
                );
                return new ApiResponse<int>(false, "No valid invitations found to cancel", 0);
            }

            var validInvitationIds = validInvitations.Select(v => v.Id).ToList();

            // 2. Actualizar usando Entity Framework
            var invitationsToUpdate = await _dbContext
                .Invitations.Where(i =>
                    validInvitationIds.Contains(i.Id) && i.Status == InvitationStatus.Pending
                )
                .ToListAsync(cancellationToken);

            if (!invitationsToUpdate.Any())
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning("No invitations found to update in bulk operation");
                return new ApiResponse<int>(false, "Failed to cancel invitations", 0);
            }

            // Actualizar cada invitación
            var updateTime = DateTime.UtcNow;
            foreach (var invitation in invitationsToUpdate)
            {
                invitation.Status = InvitationStatus.Cancelled;
                invitation.CancelledAt = updateTime;
                invitation.CancelledByUserId = request.CancelledByUserId;
                invitation.CancellationReason = request.CancelRequest.CancellationReason;
                invitation.UpdatedAt = updateTime;
            }

            var rowsAffected = await _dbContext.SaveChangesAsync(cancellationToken);

            if (rowsAffected == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning("Failed to cancel any invitations in bulk operation");
                return new ApiResponse<int>(false, "Failed to cancel invitations", 0);
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Bulk cancelled {Count} invitations by user {CancelledByUserId}. Reason: {Reason}",
                rowsAffected,
                request.CancelledByUserId,
                request.CancelRequest.CancellationReason ?? "None provided"
            );

            // 3. Publicar eventos de cancelación para cada invitación
            foreach (var invitation in validInvitations)
            {
                _eventBus.Publish(
                    new InvitationCancelledEvent(
                        Id: Guid.NewGuid(),
                        OccurredOn: DateTime.UtcNow,
                        InvitationId: invitation.Id,
                        CompanyId: invitation.CompanyId,
                        Email: invitation.Email,
                        Token: invitation.Token,
                        CancelledByUserId: request.CancelledByUserId,
                        CancellationReason: request.CancelRequest.CancellationReason,
                        CompanyName: invitation.CompanyName,
                        CompanyDomain: invitation.CompanyDomain
                    )
                );
            }

            return new ApiResponse<int>(
                true,
                $"Successfully cancelled {rowsAffected} invitation(s)",
                rowsAffected
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error in bulk cancel invitations operation");
            return new ApiResponse<int>(false, ex.Message, 0);
        }
    }
}
