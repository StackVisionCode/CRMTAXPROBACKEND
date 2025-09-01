using AuthService.Commands.InvitationCommands;
using AuthService.DTOs.UserDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;

namespace Handlers.UserHandlers;

public class ValidateInvitationHandler
    : IRequestHandler<ValidateInvitationCommand, ApiResponse<InvitationValidationDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ValidateInvitationHandler> _logger;
    private readonly IInvitationTokenService _invitationTokenService;

    public ValidateInvitationHandler(
        ApplicationDbContext dbContext,
        ILogger<ValidateInvitationHandler> logger,
        IInvitationTokenService invitationTokenService
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _invitationTokenService = invitationTokenService;
    }

    public async Task<ApiResponse<InvitationValidationDTO>> Handle(
        ValidateInvitationCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // 1. Validar token
            var (isValid, companyId, email, roleIds, errorMessage) =
                _invitationTokenService.ValidateInvitation(request.Token);

            if (!isValid)
            {
                _logger.LogWarning("Invalid invitation token provided");
                return new ApiResponse<InvitationValidationDTO>(
                    false,
                    errorMessage ?? "Invalid token",
                    new InvitationValidationDTO
                    {
                        IsValid = false,
                        ErrorMessage = errorMessage ?? "Invalid token",
                    }
                );
            }

            // 2. Verificar que la company aún existe (sin CustomPlans)
            var companyQuery =
                from c in _dbContext.Companies
                where c.Id == companyId
                select new
                {
                    c.Id,
                    c.CompanyName,
                    c.FullName,
                    c.Domain,
                    c.IsCompany,
                    c.ServiceLevel,
                    OwnerCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && u.IsOwner && u.IsActive
                    ),
                };

            var company = await companyQuery.FirstOrDefaultAsync(cancellationToken);
            if (company == null)
            {
                _logger.LogWarning(
                    "Company no longer exists for invitation token: CompanyId={CompanyId}",
                    companyId
                );
                return new ApiResponse<InvitationValidationDTO>(
                    false,
                    "Company no longer exists",
                    new InvitationValidationDTO
                    {
                        IsValid = false,
                        ErrorMessage = "Company no longer exists",
                    }
                );
            }

            // 3. Verificar que la company tenga al menos un owner activo
            if (company.OwnerCount == 0)
            {
                _logger.LogWarning(
                    "Company has no active owners for invitation: CompanyId={CompanyId}",
                    companyId
                );
                return new ApiResponse<InvitationValidationDTO>(
                    false,
                    "Company has no active administrators",
                    new InvitationValidationDTO
                    {
                        IsValid = false,
                        ErrorMessage = "Company has no active administrators",
                    }
                );
            }

            // 4. Verificar que el email no se haya registrado
            var emailExists = await _dbContext.TaxUsers.AnyAsync(
                u => u.Email == email,
                cancellationToken
            );

            if (emailExists)
            {
                _logger.LogWarning("Email already registered for invitation: Email={Email}", email);
                return new ApiResponse<InvitationValidationDTO>(
                    false,
                    "Email already registered",
                    new InvitationValidationDTO
                    {
                        IsValid = false,
                        ErrorMessage = "Email already registered",
                    }
                );
            }

            // 5. Verificar el registro de invitación en la base de datos
            var invitationQuery =
                from i in _dbContext.Invitations
                where i.Token == request.Token
                select new
                {
                    i.Id,
                    i.Status,
                    i.ExpiresAt,
                    i.Email,
                    i.CompanyId,
                };

            var invitationRecord = await invitationQuery.FirstOrDefaultAsync(cancellationToken);

            if (invitationRecord == null)
            {
                _logger.LogWarning("Invitation record not found in database for token");
                return new ApiResponse<InvitationValidationDTO>(
                    false,
                    "Invitation not found",
                    new InvitationValidationDTO
                    {
                        IsValid = false,
                        ErrorMessage = "Invitation not found",
                    }
                );
            }

            // 6. Verificar estado de la invitación
            if (invitationRecord.Status != InvitationStatus.Pending)
            {
                _logger.LogWarning(
                    "Invitation is not pending: Status={Status}, InvitationId={InvitationId}",
                    invitationRecord.Status,
                    invitationRecord.Id
                );
                return new ApiResponse<InvitationValidationDTO>(
                    false,
                    $"Invitation is {invitationRecord.Status.ToString().ToLower()}",
                    new InvitationValidationDTO
                    {
                        IsValid = false,
                        ErrorMessage =
                            $"Invitation is {invitationRecord.Status.ToString().ToLower()}",
                    }
                );
            }

            // 7. Verificar expiración
            if (invitationRecord.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogWarning(
                    "Invitation has expired: ExpiresAt={ExpiresAt}, InvitationId={InvitationId}",
                    invitationRecord.ExpiresAt,
                    invitationRecord.Id
                );

                // Marcar como expirada
                var expiredInvitation = await _dbContext.Invitations.FirstOrDefaultAsync(
                    i => i.Id == invitationRecord.Id,
                    cancellationToken
                );

                if (expiredInvitation != null)
                {
                    expiredInvitation.Status = InvitationStatus.Expired;
                    expiredInvitation.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                return new ApiResponse<InvitationValidationDTO>(
                    false,
                    "Invitation has expired",
                    new InvitationValidationDTO
                    {
                        IsValid = false,
                        ErrorMessage = "Invitation has expired",
                    }
                );
            }

            // 8. Token válido y todo OK
            _logger.LogInformation(
                "Invitation validated successfully: Email={Email}, CompanyId={CompanyId} (ServiceLevel: {ServiceLevel})",
                email,
                companyId,
                company.ServiceLevel
            );

            return new ApiResponse<InvitationValidationDTO>(
                true,
                "Valid invitation",
                new InvitationValidationDTO
                {
                    IsValid = true,
                    Email = email,
                    CompanyId = companyId,
                    CompanyName = company.CompanyName,
                    CompanyFullName = company.FullName,
                    CompanyDomain = company.Domain,
                    IsCompany = company.IsCompany,
                    ServiceLevel = company.ServiceLevel,
                    ExpiresAt = invitationRecord.ExpiresAt,
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating invitation token: {Token}", request.Token);
            return new ApiResponse<InvitationValidationDTO>(
                false,
                "Validation failed",
                new InvitationValidationDTO { IsValid = false, ErrorMessage = "Validation failed" }
            );
        }
    }
}
