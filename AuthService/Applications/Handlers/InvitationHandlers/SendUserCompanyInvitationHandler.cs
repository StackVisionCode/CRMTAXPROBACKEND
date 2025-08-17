using Applications.Common;
using AuthService.Commands.InvitationCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.InvitationEvents;

namespace Handlers.UserHandlers;

public class SendUserInvitationHandler
    : IRequestHandler<SendUserInvitationCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SendUserInvitationHandler> _logger;
    private readonly IInvitationTokenService _invitationTokenService;
    private readonly IEventBus _eventBus;
    private readonly LinkBuilder _linkBuilder;

    public SendUserInvitationHandler(
        ApplicationDbContext dbContext,
        ILogger<SendUserInvitationHandler> logger,
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
        SendUserInvitationCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var invitation = request.Invitation;

            // 1. Verificar que la company existe y obtener información
            var companyQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                where c.Id == invitation.CompanyId
                select new
                {
                    c.Id,
                    c.CompanyName,
                    c.FullName,
                    c.Domain,
                    c.IsCompany,
                    CustomPlanId = cp.Id,
                    CustomPlanIsActive = cp.IsActive,
                    UserLimit = cp.UserLimit,
                    CurrentActiveUserCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == invitation.CompanyId && u.IsActive
                    ),
                };

            var company = await companyQuery.FirstOrDefaultAsync(cancellationToken);
            if (company == null)
            {
                _logger.LogWarning("Company not found: {CompanyId}", invitation.CompanyId);
                return new ApiResponse<bool>(false, "Company not found", false);
            }

            if (!company.CustomPlanIsActive)
            {
                _logger.LogWarning("Company plan is inactive: {CompanyId}", invitation.CompanyId);
                return new ApiResponse<bool>(false, "Company plan is inactive", false);
            }

            // 2. Verificar límites de usuarios basado en CustomPlan
            if (company.CurrentActiveUserCount >= company.UserLimit)
            {
                _logger.LogWarning(
                    "User limit exceeded for company: {CompanyId}. "
                        + "Current active users: {Current}, CustomPlan limit: {Limit}",
                    invitation.CompanyId,
                    company.CurrentActiveUserCount,
                    company.UserLimit
                );
                return new ApiResponse<bool>(
                    false,
                    $"User limit exceeded. Current: {company.CurrentActiveUserCount}, Limit: {company.UserLimit}",
                    false
                );
            }

            // 3. Verificar que el email no esté ya registrado
            var emailExists = await _dbContext.TaxUsers.AnyAsync(
                u => u.Email == invitation.Email,
                cancellationToken
            );

            if (emailExists)
            {
                _logger.LogWarning("Email already exists in the system: {Email}", invitation.Email);
                return new ApiResponse<bool>(false, "Email already exists in the system", false);
            }

            // 4. Validar roles si se especifican (no permitir roles de Administrator/Developer)
            if (invitation.RoleIds?.Any() == true)
            {
                var validRoleCount = await _dbContext.Roles.CountAsync(
                    r =>
                        invitation.RoleIds.Contains(r.Id)
                        && !r.Name.Contains("Administrator")
                        && !r.Name.Contains("Developer"),
                    cancellationToken
                );

                if (validRoleCount != invitation.RoleIds.Count)
                {
                    _logger.LogWarning(
                        "Some role IDs are invalid or not allowed for invitation to {Email}",
                        invitation.Email
                    );
                    return new ApiResponse<bool>(
                        false,
                        "Some specified roles are invalid or not allowed",
                        false
                    );
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
                new UserInvitationSentEvent(
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
