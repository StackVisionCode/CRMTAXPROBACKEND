using AuthService.DTOs.InvitationDTOs;
using Common;
using Infraestructure.Context;
using Infraestructure.Queries.InvitationsQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.InvitationHandlers;

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
            // Query principal con datos de company y estadísticas
            var companyDataQuery =
                from c in _dbContext.Companies
                where c.Id == request.CompanyId
                select new
                {
                    CompanyId = c.Id,
                    CompanyName = c.CompanyName ?? c.FullName,
                    ServiceLevel = c.ServiceLevel,
                    IsCompany = c.IsCompany,

                    // Estadísticas de usuarios
                    CurrentActiveUsers = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == request.CompanyId && u.IsActive
                    ),
                    CurrentTotalUsers = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == request.CompanyId
                    ),
                    OwnerCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == request.CompanyId && u.IsOwner && u.IsActive
                    ),

                    // Estadísticas de invitaciones
                    PendingInvitations = _dbContext.Invitations.Count(i =>
                        i.CompanyId == request.CompanyId
                        && i.Status == InvitationStatus.Pending
                        && i.ExpiresAt > DateTime.UtcNow
                    ),
                    ExpiredInvitations = _dbContext.Invitations.Count(i =>
                        i.CompanyId == request.CompanyId
                        && (i.Status == InvitationStatus.Expired || i.ExpiresAt <= DateTime.UtcNow)
                    ),
                    AcceptedInvitations = _dbContext.Invitations.Count(i =>
                        i.CompanyId == request.CompanyId && i.Status == InvitationStatus.Accepted
                    ),
                    TotalInvitationsSent = _dbContext.Invitations.Count(i =>
                        i.CompanyId == request.CompanyId
                    ),
                };

            var companyData = await companyDataQuery.FirstOrDefaultAsync(cancellationToken);

            if (companyData == null)
            {
                _logger.LogWarning(
                    "Company not found for invitation limit check: {CompanyId}",
                    request.CompanyId
                );
                return new ApiResponse<InvitationLimitCheckDTO>(false, "Company not found", null!);
            }

            // Validaciones básicas que puede hacer AuthService
            var validationResults = PerformBasicValidation(companyData);

            var result = new InvitationLimitCheckDTO
            {
                CompanyId = request.CompanyId,
                CompanyName = companyData.CompanyName,
                ServiceLevel = companyData.ServiceLevel,
                IsCompany = companyData.IsCompany,

                // Estadísticas de usuarios
                CurrentActiveUsers = companyData.CurrentActiveUsers,
                CurrentTotalUsers = companyData.CurrentTotalUsers,
                OwnerCount = companyData.OwnerCount,

                // Estadísticas de invitaciones
                PendingInvitations = companyData.PendingInvitations,
                ExpiredInvitations = companyData.ExpiredInvitations,
                AcceptedInvitations = companyData.AcceptedInvitations,
                TotalInvitationsSent = companyData.TotalInvitationsSent,

                // Validación básica
                CanSendBasicValidation = validationResults.CanSend,
                BasicValidationMessage = validationResults.Message,

                // Metadata
                RequiresSubscriptionCheck = true,
                LastCheckedAt = DateTime.UtcNow,
            };

            _logger.LogInformation(
                "Invitation limit check completed for company {CompanyId} (ServiceLevel: {ServiceLevel}): "
                    + "BasicValidation={BasicValidation}, ActiveUsers={ActiveUsers}, PendingInvitations={PendingInvitations}, "
                    + "OwnerCount={OwnerCount}, TotalInvitations={TotalInvitations}",
                request.CompanyId,
                companyData.ServiceLevel,
                validationResults.CanSend,
                companyData.CurrentActiveUsers,
                companyData.PendingInvitations,
                companyData.OwnerCount,
                companyData.TotalInvitationsSent
            );

            return new ApiResponse<InvitationLimitCheckDTO>(
                true,
                "Invitation limit check completed successfully",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking invitation limits for CompanyId: {CompanyId}",
                request.CompanyId
            );
            return new ApiResponse<InvitationLimitCheckDTO>(
                false,
                "Error checking invitation limits",
                null!
            );
        }
    }

    private (bool CanSend, string Message) PerformBasicValidation(dynamic companyData)
    {
        // Validación 1: Debe tener al menos un owner activo
        if (companyData.OwnerCount == 0)
        {
            return (
                false,
                "Company has no active administrators. An active administrator is required to send invitations."
            );
        }

        // Validación 2: Company debe estar en estado válido (siempre verdadero si existe en DB)
        if (string.IsNullOrEmpty(companyData.CompanyName))
        {
            return (false, "Company information is incomplete.");
        }

        // Validación 3: Verificar si hay demasiadas invitaciones pendientes (límite interno básico)
        const int MAX_PENDING_INVITATIONS = 50; // Límite interno de AuthService
        if (companyData.PendingInvitations >= MAX_PENDING_INVITATIONS)
        {
            return (
                false,
                $"Too many pending invitations ({companyData.PendingInvitations}). Please wait for some to be accepted or expire before sending more."
            );
        }

        // Todas las validaciones básicas pasaron
        return (
            true,
            "Basic validation passed. Check subscription limits for complete validation."
        );
    }
}
