using AuthService.Commands.InvitationCommands;
using AuthService.DTOs.UserCompanyDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;

namespace AuthService.Handlers.InvitationHandlers;

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
                return new ApiResponse<InvitationValidationDTO>(
                    false,
                    errorMessage ?? "Invalid token",
                    new InvitationValidationDTO { IsValid = false, ErrorMessage = errorMessage }
                );
            }

            // 2. Verificar que la company aún existe
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
                };

            var company = await companyQuery.FirstOrDefaultAsync(cancellationToken);
            if (company == null)
            {
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

            // 3. Verificar que el email no se haya registrado
            var emailExists =
                await _dbContext.UserCompanies.AnyAsync(uc => uc.Email == email, cancellationToken)
                || await _dbContext.TaxUsers.AnyAsync(u => u.Email == email, cancellationToken);

            if (emailExists)
            {
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

            // 4. Token válido y todo OK
            return new ApiResponse<InvitationValidationDTO>(
                true,
                "Valid invitation",
                new InvitationValidationDTO
                {
                    IsValid = true,
                    Email = email,
                    CompanyId = companyId,
                    CompanyName = company.CompanyName,
                    CompanyDomain = company.Domain,
                    IsCompany = company.IsCompany,
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating invitation token");
            return new ApiResponse<InvitationValidationDTO>(
                false,
                "Validation failed",
                new InvitationValidationDTO { IsValid = false, ErrorMessage = "Validation failed" }
            );
        }
    }
}
