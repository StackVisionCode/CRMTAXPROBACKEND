using AuthService.Commands.InvitationCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.InvitationEvents;

namespace Handlers.InvitationHandlers;

/// <summary>
/// Handler para cancelar una invitación específica
/// </summary>
public class CancelInvitationHandler : IRequestHandler<CancelInvitationCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CancelInvitationHandler> _logger;
    private readonly IEventBus _eventBus;

    public CancelInvitationHandler(
        ApplicationDbContext dbContext,
        ILogger<CancelInvitationHandler> logger,
        IEventBus eventBus
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<ApiResponse<bool>> Handle(
        CancelInvitationCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Obtener la invitación con información de company
            var invitationQuery =
                from i in _dbContext.Invitations
                join c in _dbContext.Companies on i.CompanyId equals c.Id
                where i.Id == request.CancelRequest.InvitationId
                select new
                {
                    i.Id,
                    i.CompanyId,
                    i.Email,
                    i.Status,
                    i.ExpiresAt,
                    i.Token,
                    CompanyName = c.CompanyName,
                    CompanyDomain = c.Domain,
                };

            var invitationData = await invitationQuery.FirstOrDefaultAsync(cancellationToken);

            if (invitationData == null)
            {
                _logger.LogWarning(
                    "Invitation not found for cancellation: {InvitationId}",
                    request.CancelRequest.InvitationId
                );
                return new ApiResponse<bool>(false, "Invitation not found", false);
            }

            // 2. Verificar que el usuario que cancela pertenece a la misma company
            var userHasAccess = await _dbContext.TaxUsers.AnyAsync(
                u =>
                    u.Id == request.CancelledByUserId
                    && u.CompanyId == invitationData.CompanyId
                    && u.IsActive,
                cancellationToken
            );

            if (!userHasAccess)
            {
                _logger.LogWarning(
                    "User {CancelledByUserId} doesn't have access to cancel invitation {InvitationId}",
                    request.CancelledByUserId,
                    request.CancelRequest.InvitationId
                );
                return new ApiResponse<bool>(false, "Access denied", false);
            }

            // 3. Verificar que la invitación puede ser cancelada
            if (invitationData.Status != InvitationStatus.Pending)
            {
                _logger.LogWarning(
                    "Cannot cancel invitation with status {Status}: {InvitationId}",
                    invitationData.Status,
                    request.CancelRequest.InvitationId
                );
                return new ApiResponse<bool>(
                    false,
                    $"Cannot cancel invitation with status: {invitationData.Status}",
                    false
                );
            }

            // 4. Actualizar la invitación
            var invitationToUpdate = await _dbContext.Invitations.FirstOrDefaultAsync(
                i =>
                    i.Id == request.CancelRequest.InvitationId
                    && i.Status == InvitationStatus.Pending,
                cancellationToken
            );

            if (invitationToUpdate == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning(
                    "Invitation not found or already processed: {InvitationId}",
                    request.CancelRequest.InvitationId
                );
                return new ApiResponse<bool>(
                    false,
                    "Invitation not found or already processed",
                    false
                );
            }

            // Actualizar la invitación
            invitationToUpdate.Status = InvitationStatus.Cancelled;
            invitationToUpdate.CancelledAt = DateTime.UtcNow;
            invitationToUpdate.CancelledByUserId = request.CancelledByUserId;
            invitationToUpdate.CancellationReason = request.CancelRequest.CancellationReason;
            invitationToUpdate.UpdatedAt = DateTime.UtcNow;

            var rowsAffected = await _dbContext.SaveChangesAsync(cancellationToken);

            if (rowsAffected == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning(
                    "Failed to cancel invitation: {InvitationId}",
                    request.CancelRequest.InvitationId
                );
                return new ApiResponse<bool>(false, "Failed to cancel invitation", false);
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Invitation cancelled successfully: {InvitationId} by user {CancelledByUserId}. Reason: {Reason}",
                request.CancelRequest.InvitationId,
                request.CancelledByUserId,
                request.CancelRequest.CancellationReason ?? "None provided"
            );

            // 5. Publicar evento de cancelación
            _eventBus.Publish(
                new InvitationCancelledEvent(
                    Id: Guid.NewGuid(),
                    OccurredOn: DateTime.UtcNow,
                    InvitationId: request.CancelRequest.InvitationId,
                    CompanyId: invitationData.CompanyId,
                    Email: invitationData.Email,
                    Token: invitationData.Token,
                    CancelledByUserId: request.CancelledByUserId,
                    CancellationReason: request.CancelRequest.CancellationReason,
                    CompanyName: invitationData.CompanyName,
                    CompanyDomain: invitationData.CompanyDomain
                )
            );

            return new ApiResponse<bool>(true, "Invitation cancelled successfully", true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error cancelling invitation: {InvitationId}",
                request.CancelRequest.InvitationId
            );
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
