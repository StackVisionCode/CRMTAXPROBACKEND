using Applications.Common;
using AuthService.Applications.Common;
using AuthService.Commands.InvitationCommands;
using AuthService.Domains.Invitations;
using AuthService.DTOs.InvitationDTOs;
using AuthService.Infraestructure.Services;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.InvitationEvents;

namespace Handlers.InvitationHandlers;

public class SendUserInvitationHandler
    : IRequestHandler<SendUserInvitationCommand, ApiResponse<InvitationDTO>>
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

    public async Task<ApiResponse<InvitationDTO>> Handle(
        SendUserInvitationCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var invitation = request.Invitation;

            // 1. Verificar usuario que envía la invitación
            var invitingUserValidation = await ValidateInvitingUserAsync(
                request.InvitedByUserId,
                invitation.CompanyId,
                cancellationToken
            );

            if (!invitingUserValidation.IsValid)
            {
                return new ApiResponse<InvitationDTO>(false, invitingUserValidation.Message, null!);
            }

            var invitingUser = invitingUserValidation.User!;

            // 2. Verificar información de la company
            var companyValidation = await ValidateCompanyAsync(
                invitation.CompanyId,
                cancellationToken
            );

            if (!companyValidation.IsValid)
            {
                return new ApiResponse<InvitationDTO>(false, companyValidation.Message, null!);
            }

            var companyInfo = companyValidation.CompanyInfo!;

            // 3. Validaciones básicas de invitación
            var basicValidation = await PerformBasicInvitationValidationAsync(
                invitation,
                companyInfo,
                cancellationToken
            );

            if (!basicValidation.IsValid)
            {
                return new ApiResponse<InvitationDTO>(false, basicValidation.Message, null!);
            }

            // 4. Logging de procesamiento
            _logger.LogInformation(
                "Processing invitation for company {CompanyId} (ServiceLevel: {ServiceLevel}). "
                    + "Current users: {ActiveUsers}, Pending invitations: {PendingInvitations}",
                invitation.CompanyId,
                companyInfo.ServiceLevel,
                companyInfo.CurrentActiveUsers,
                companyInfo.PendingInvitations
            );

            // 5. Validar roles si se especifican
            if (invitation.RoleIds?.Any() == true)
            {
                var roleValidation = await ValidateRolesForInvitationAsync(
                    invitation.RoleIds,
                    companyInfo.ServiceLevel,
                    cancellationToken
                );

                if (!roleValidation.IsValid)
                {
                    return new ApiResponse<InvitationDTO>(false, roleValidation.Message, null!);
                }
            }

            // 6. Generar token y crear invitación
            var (token, expiration) = _invitationTokenService.GenerateInvitation(
                invitation.CompanyId,
                invitation.Email,
                invitation.RoleIds
            );

            var invitationLink = _linkBuilder.BuildInvitationLink(request.Origin, token);

            var invitationEntity = new Invitation
            {
                Id = Guid.NewGuid(),
                CompanyId = invitation.CompanyId,
                InvitedByUserId = request.InvitedByUserId,
                Email = invitation.Email,
                Token = token,
                ExpiresAt = expiration,
                Status = InvitationStatus.Pending,
                PersonalMessage = invitation.PersonalMessage,
                RoleIds = invitation.RoleIds ?? new List<Guid>(),
                InvitationLink = invitationLink,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                CreatedAt = DateTime.UtcNow,
            };

            await _dbContext.Invitations.AddAsync(invitationEntity, cancellationToken);
            var saveResult = await _dbContext.SaveChangesAsync(cancellationToken);

            if (saveResult <= 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to save invitation to database");
                return new ApiResponse<InvitationDTO>(false, "Failed to create invitation", null!);
            }

            await transaction.CommitAsync(cancellationToken);

            // 7. Logging de éxito
            _logger.LogInformation(
                "Invitation created successfully: Id={InvitationId}, Email={Email}, CompanyId={CompanyId} (ServiceLevel: {ServiceLevel}), "
                    + "InvitedBy={InvitedByUserId}, Expires={ExpiresAt}",
                invitationEntity.Id,
                invitation.Email,
                invitation.CompanyId,
                companyInfo.ServiceLevel,
                request.InvitedByUserId,
                expiration
            );

            // 8. Publicar evento para envío de email
            _eventBus.Publish(
                new UserInvitationSentEvent(
                    Id: Guid.NewGuid(),
                    OccurredOn: DateTime.UtcNow,
                    CompanyId: invitation.CompanyId,
                    Email: invitation.Email,
                    InvitationLink: invitationLink,
                    ExpiresAt: expiration,
                    CompanyName: companyInfo.CompanyName,
                    CompanyFullName: companyInfo.CompanyFullName,
                    CompanyDomain: companyInfo.CompanyDomain,
                    IsCompany: companyInfo.IsCompany,
                    PersonalMessage: invitation.PersonalMessage
                )
            );

            // 9. Preparar respuesta
            var responseDto = await BuildInvitationResponseAsync(
                invitationEntity,
                invitingUser,
                companyInfo,
                cancellationToken
            );

            return new ApiResponse<InvitationDTO>(
                true,
                "Invitation sent successfully",
                responseDto
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error sending invitation: {Message}", ex.Message);
            return new ApiResponse<InvitationDTO>(false, "Error sending invitation", null!);
        }
    }

    #region Helper Methods

    private async Task<(
        bool IsValid,
        string Message,
        InvitingUserInfo? User
    )> ValidateInvitingUserAsync(Guid userId, Guid companyId, CancellationToken cancellationToken)
    {
        var invitingUserQuery =
            from u in _dbContext.TaxUsers
            where u.Id == userId && u.CompanyId == companyId
            select new InvitingUserInfo
            {
                Id = u.Id,
                Email = u.Email,
                Name = u.Name,
                LastName = u.LastName,
                IsOwner = u.IsOwner,
                IsActive = u.IsActive,
                CompanyId = u.CompanyId,
            };

        var invitingUser = await invitingUserQuery.FirstOrDefaultAsync(cancellationToken);

        if (invitingUser == null)
        {
            _logger.LogWarning(
                "User not found or doesn't belong to company: UserId={UserId}, CompanyId={CompanyId}",
                userId,
                companyId
            );
            return (false, "User not found or insufficient permissions", null);
        }

        if (!invitingUser.IsActive)
        {
            _logger.LogWarning("Inactive user trying to send invitation: UserId={UserId}", userId);
            return (false, "User account is inactive", null);
        }

        return (true, "User validation passed", invitingUser);
    }

    private async Task<(
        bool IsValid,
        string Message,
        CompanyInfo? CompanyInfo
    )> ValidateCompanyAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var companyInfoQuery =
            from c in _dbContext.Companies
            where c.Id == companyId
            select new CompanyInfo
            {
                CompanyId = c.Id,
                CompanyName = c.CompanyName,
                CompanyFullName = c.FullName,
                CompanyDomain = c.Domain,
                IsCompany = c.IsCompany,
                ServiceLevel = c.ServiceLevel,
                CurrentActiveUsers = _dbContext.TaxUsers.Count(u =>
                    u.CompanyId == companyId && u.IsActive
                ),
                OwnerCount = _dbContext.TaxUsers.Count(u =>
                    u.CompanyId == companyId && u.IsOwner && u.IsActive
                ),
                PendingInvitations = _dbContext.Invitations.Count(i =>
                    i.CompanyId == companyId
                    && i.Status == InvitationStatus.Pending
                    && i.ExpiresAt > DateTime.UtcNow
                ),
            };

        var companyInfo = await companyInfoQuery.FirstOrDefaultAsync(cancellationToken);

        if (companyInfo == null)
        {
            _logger.LogWarning("Company not found: {CompanyId}", companyId);
            return (false, "Company not found", null);
        }

        if (companyInfo.OwnerCount == 0)
        {
            _logger.LogWarning("Company has no active owners: {CompanyId}", companyId);
            return (false, "Company has no active administrators", null);
        }

        return (true, "Company validation passed", companyInfo);
    }

    private async Task<(bool IsValid, string Message)> PerformBasicInvitationValidationAsync(
        NewInvitationDTO invitation,
        CompanyInfo companyInfo,
        CancellationToken cancellationToken
    )
    {
        // 1. Verificar que el email no esté ya registrado
        var emailExists = await _dbContext.TaxUsers.AnyAsync(
            u => u.Email == invitation.Email,
            cancellationToken
        );

        if (emailExists)
        {
            _logger.LogWarning("Email already registered: {Email}", invitation.Email);
            return (false, "Email already registered in the system");
        }

        // 2. Verificar que no exista una invitación pendiente
        var pendingInvitationExists = await _dbContext.Invitations.AnyAsync(
            i =>
                i.CompanyId == invitation.CompanyId
                && i.Email == invitation.Email
                && i.Status == InvitationStatus.Pending
                && i.ExpiresAt > DateTime.UtcNow,
            cancellationToken
        );

        if (pendingInvitationExists)
        {
            _logger.LogWarning(
                "Pending invitation already exists: Email={Email}, CompanyId={CompanyId}",
                invitation.Email,
                invitation.CompanyId
            );
            return (false, "A pending invitation already exists for this email");
        }

        // 3. Validación básica de límite interno (anti-spam)
        const int MAX_PENDING_INVITATIONS = 50;
        if (companyInfo.PendingInvitations >= MAX_PENDING_INVITATIONS)
        {
            _logger.LogWarning(
                "Too many pending invitations for company: {CompanyId}, Count: {Count}",
                invitation.CompanyId,
                companyInfo.PendingInvitations
            );
            return (
                false,
                $"Too many pending invitations ({companyInfo.PendingInvitations}). Please wait for some to be accepted or expire."
            );
        }

        return (true, "Basic validation passed");
    }

    private async Task<(bool IsValid, string Message)> ValidateRolesForInvitationAsync(
        ICollection<Guid> roleIds,
        ServiceLevel companyServiceLevel,
        CancellationToken cancellationToken
    )
    {
        var rolesQuery =
            from r in _dbContext.Roles
            where roleIds.Contains(r.Id)
            select new
            {
                r.Id,
                r.Name,
                r.ServiceLevel,
            };

        var roles = await rolesQuery.ToListAsync(cancellationToken);

        if (roles.Count != roleIds.Count)
        {
            _logger.LogWarning("Some role IDs not found: {RoleIds}", string.Join(", ", roleIds));
            return (false, "Some specified roles were not found");
        }

        foreach (var role in roles)
        {
            // No permitir roles de Administrator o Developer en invitaciones
            if (role.Name.Contains("Administrator") || role.Name.Contains("Developer"))
            {
                _logger.LogWarning(
                    "Attempted to assign restricted role via invitation: {RoleName}",
                    role.Name
                );
                return (false, $"Role '{role.Name}' cannot be assigned via invitation");
            }

            // Validar ServiceLevel
            if (role.ServiceLevel.HasValue && role.ServiceLevel > companyServiceLevel)
            {
                return (
                    false,
                    $"Role '{role.Name}' requires {role.ServiceLevel} service level, but company has {companyServiceLevel}"
                );
            }
        }

        return (true, "Role validation passed");
    }

    private async Task<InvitationDTO> BuildInvitationResponseAsync(
        Invitation invitationEntity,
        InvitingUserInfo invitingUser,
        CompanyInfo companyInfo,
        CancellationToken cancellationToken
    )
    {
        // Obtener nombres de roles si se especificaron
        var roleNames = new List<string>();
        if (invitationEntity.RoleIds?.Any() == true)
        {
            roleNames = await _dbContext
                .Roles.Where(r => invitationEntity.RoleIds.Contains(r.Id))
                .Select(r => r.Name)
                .ToListAsync(cancellationToken);
        }

        return new InvitationDTO
        {
            Id = invitationEntity.Id,
            CompanyId = invitationEntity.CompanyId,
            InvitedByUserId = invitationEntity.InvitedByUserId,
            Email = invitationEntity.Email,
            Token = invitationEntity.Token,
            ExpiresAt = invitationEntity.ExpiresAt,
            Status = invitationEntity.Status,
            PersonalMessage = invitationEntity.PersonalMessage,
            RoleIds = invitationEntity.RoleIds ?? new List<Guid>(),
            CreatedAt = invitationEntity.CreatedAt,
            InvitationLink = invitationEntity.InvitationLink,
            IpAddress = invitationEntity.IpAddress,
            UserAgent = invitationEntity.UserAgent,

            // Información del usuario que invitó
            InvitedByUserName = invitingUser.Name ?? string.Empty,
            InvitedByUserLastName = invitingUser.LastName ?? string.Empty,
            InvitedByUserEmail = invitingUser.Email,
            InvitedByUserIsOwner = invitingUser.IsOwner,

            // Información de la company
            CompanyName = companyInfo.CompanyName,
            CompanyFullName = companyInfo.CompanyFullName,
            CompanyDomain = companyInfo.CompanyDomain,
            CompanyIsCompany = companyInfo.IsCompany,
            CompanyServiceLevel = companyInfo.ServiceLevel,

            RoleNames = roleNames,
        };
    }

    #endregion

    #region Helper Classes

    private class InvitingUserInfo
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? LastName { get; set; }
        public bool IsOwner { get; set; }
        public bool IsActive { get; set; }
        public Guid CompanyId { get; set; }
    }

    private class CompanyInfo
    {
        public Guid CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyFullName { get; set; }
        public string? CompanyDomain { get; set; }
        public bool IsCompany { get; set; }
        public ServiceLevel ServiceLevel { get; set; }
        public int CurrentActiveUsers { get; set; }
        public int OwnerCount { get; set; }
        public int PendingInvitations { get; set; }
    }

    #endregion
}
