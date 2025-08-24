using AuthService.DTOs.InvitationDTOs;
using Common;
using Infraestructure.Context;
using Infraestructure.Queries.InvitationsQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.InvitationHandlers;

/// <summary>
/// Handler para verificar límites de invitaciones
/// </summary>
public class CanSendMoreInvitationsHandler
    : IRequestHandler<CanSendMoreInvitationsQuery, ApiResponse<InvitationLimitCheckDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CanSendMoreInvitationsHandler> _logger;

    public CanSendMoreInvitationsHandler(
        ApplicationDbContext dbContext,
        ILogger<CanSendMoreInvitationsHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<InvitationLimitCheckDTO>> Handle(
        CanSendMoreInvitationsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var limitCheckQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                where c.Id == request.CompanyId
                select new
                {
                    CompanyId = c.Id,
                    UserLimit = cp.UserLimit,
                    CurrentActiveUsers = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == request.CompanyId && u.IsActive
                    ),
                    PendingInvitations = _dbContext.Invitations.Count(i =>
                        i.CompanyId == request.CompanyId
                        && i.Status == InvitationStatus.Pending
                        && i.ExpiresAt > DateTime.UtcNow
                    ),
                };

            var limitData = await limitCheckQuery.FirstOrDefaultAsync(cancellationToken);

            if (limitData == null)
            {
                _logger.LogWarning(
                    "Company not found for limit check: {CompanyId}",
                    request.CompanyId
                );
                return new ApiResponse<InvitationLimitCheckDTO>(false, "Company not found", null!);
            }

            var availableSlots = limitData.UserLimit - limitData.CurrentActiveUsers;
            var remainingInvitations = Math.Max(0, availableSlots - limitData.PendingInvitations);
            var canSendMore = remainingInvitations > 0;

            var limitMessage =
                canSendMore ? $"You can send {remainingInvitations} more invitation(s)"
                : limitData.PendingInvitations > 0
                    ? $"User limit reached. You have {limitData.PendingInvitations} pending invitation(s)"
                : "User limit reached. Cannot send more invitations";

            var result = new InvitationLimitCheckDTO
            {
                CompanyId = request.CompanyId,
                CanSendMore = canSendMore,
                CurrentActiveUsers = limitData.CurrentActiveUsers,
                CustomPlanUserLimit = limitData.UserLimit,
                PendingInvitations = limitData.PendingInvitations,
                AvailableSlots = availableSlots,
                RemainingInvitationsAllowed = remainingInvitations,
                LimitMessage = limitMessage,
            };

            return new ApiResponse<InvitationLimitCheckDTO>(true, "Limit check completed", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking invitation limits for CompanyId: {CompanyId}",
                request.CompanyId
            );
            return new ApiResponse<InvitationLimitCheckDTO>(false, ex.Message, null!);
        }
    }
}
