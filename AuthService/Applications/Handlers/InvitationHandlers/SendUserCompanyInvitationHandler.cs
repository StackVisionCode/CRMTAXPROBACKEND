using Applications.Common;
using AuthService.Commands.InvitationCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.InvitationEvents;

namespace AuthService.Handlers.InvitationHandlers;

public class SendUserCompanyInvitationHandler
    : IRequestHandler<SendUserCompanyInvitationCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SendUserCompanyInvitationHandler> _logger;
    private readonly IInvitationTokenService _invitationTokenService;
    private readonly IEventBus _eventBus;
    private readonly LinkBuilder _linkBuilder;

    public SendUserCompanyInvitationHandler(
        ApplicationDbContext dbContext,
        ILogger<SendUserCompanyInvitationHandler> logger,
        IInvitationTokenService invitationTokenService,
        IEventBus eventBus,
        LinkBuilder linkBuilder
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _invitationTokenService = invitationTokenService;
        _eventBus = eventBus;
        _linkBuilder = linkBuilder;
    }

    public async Task<ApiResponse<bool>> Handle(
        SendUserCompanyInvitationCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var invitation = request.Invitation;

            // 1. Verificar que la company existe y obtener información
            var companyQuery =
                from c in _dbContext.Companies
                where c.Id == invitation.CompanyId
                select new
                {
                    c.Id,
                    c.CompanyName,
                    c.FullName,
                    c.Domain,
                    c.IsCompany,
                    c.CustomPlan,
                    CurrentUserCompanyCount = c.UserCompanies.Count(),
                };

            var company = await companyQuery.FirstOrDefaultAsync(cancellationToken);
            if (company == null)
            {
                _logger.LogWarning("Company not found: {CompanyId}", invitation.CompanyId);
                return new ApiResponse<bool>(false, "Company not found", false);
            }

            // 2. Verificar límites de usuarios basado en CustomPlan
            var serviceQuery =
                from cm in _dbContext.CustomModules
                join m in _dbContext.Modules on cm.ModuleId equals m.Id
                join s in _dbContext.Services on m.ServiceId equals s.Id
                where cm.CustomPlanId == company.CustomPlan.Id && cm.IsIncluded
                select s.UserLimit;

            var userLimit = await serviceQuery.FirstOrDefaultAsync(cancellationToken);
            if (userLimit > 0 && company.CurrentUserCompanyCount >= userLimit)
            {
                _logger.LogWarning(
                    "User limit exceeded for company: {CompanyId}. Current: {Current}, Limit: {Limit}",
                    invitation.CompanyId,
                    company.CurrentUserCompanyCount,
                    userLimit
                );
                return new ApiResponse<bool>(false, "User limit exceeded for this company", false);
            }

            // 3. Verificar que el email no esté ya registrado en TaxUsers o UserCompanies
            var emailExistsInTaxUsers = await _dbContext.TaxUsers.AnyAsync(
                u => u.Email == invitation.Email,
                cancellationToken
            );

            var emailExistsInUserCompanies = await _dbContext.UserCompanies.AnyAsync(
                uc => uc.Email == invitation.Email,
                cancellationToken
            );

            if (emailExistsInTaxUsers || emailExistsInUserCompanies)
            {
                _logger.LogWarning("Email already exists in the system: {Email}", invitation.Email);
                return new ApiResponse<bool>(false, "Email already exists in the system", false);
            }

            // 4. Validar roles si se especifican
            if (invitation.RoleIds?.Any() == true)
            {
                var validRoleCount = await _dbContext.Roles.CountAsync(
                    r => invitation.RoleIds.Contains(r.Id),
                    cancellationToken
                );

                if (validRoleCount != invitation.RoleIds.Count)
                {
                    _logger.LogWarning(
                        "Some role IDs are invalid for invitation to {Email}",
                        invitation.Email
                    );
                    return new ApiResponse<bool>(false, "Some specified roles are invalid", false);
                }
            }

            // 5. Generar token de invitación
            var (token, expiration) = _invitationTokenService.GenerateInvitation(
                invitation.CompanyId,
                invitation.Email,
                invitation.RoleIds
            );

            // 6. Construir link de invitación
            var invitationLink = _linkBuilder.BuildInvitationLink(request.Origin, token);

            _logger.LogInformation(
                "Invitation sent to {Email} for company {CompanyId}",
                invitation.Email,
                invitation.CompanyId
            );

            // 7. Publicar evento para envío de email
            _eventBus.Publish(
                new UserCompanyInvitationSentEvent(
                    Id: Guid.NewGuid(),
                    OccurredOn: DateTime.UtcNow,
                    CompanyId: invitation.CompanyId,
                    Email: invitation.Email,
                    InvitationLink: invitationLink,
                    ExpiresAt: expiration,
                    CompanyName: company.CompanyName,
                    CompanyFullName: company.FullName,
                    CompanyDomain: company.Domain,
                    IsCompany: company.IsCompany,
                    PersonalMessage: invitation.PersonalMessage
                )
            );

            return new ApiResponse<bool>(true, "Invitation sent successfully", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invitation: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
