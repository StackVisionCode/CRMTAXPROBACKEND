using Applications.Common;
using AuthService.Commands.InvitationCommands;
using AuthService.Domains.Invitations;
using AuthService.DTOs.InvitationDTOs;
using AuthService.Infraestructure.Services;
using AutoMapper;
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
    private readonly IMapper _mapper;

    public SendUserInvitationHandler(
        ApplicationDbContext dbContext,
        ILogger<SendUserInvitationHandler> logger,
        IInvitationTokenService invitationTokenService,
        IEventBus eventBus,
        LinkBuilder linkBuilder,
        IMapper mapper
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _invitationTokenService = invitationTokenService;
        _eventBus = eventBus;
        _linkBuilder = linkBuilder;
        _mapper = mapper;
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

            // 1. Verificar que el usuario que envía la invitación existe y tiene permisos
            var invitingUserQuery =
                from u in _dbContext.TaxUsers
                where u.Id == request.InvitedByUserId && u.CompanyId == invitation.CompanyId
                select new
                {
                    u.Id,
                    u.Email,
                    u.Name,
                    u.LastName,
                    u.IsOwner,
                    u.IsActive,
                    CompanyId = u.CompanyId,
                };

            var invitingUser = await invitingUserQuery.FirstOrDefaultAsync(cancellationToken);
            if (invitingUser == null)
            {
                _logger.LogWarning(
                    "User not found or doesn't belong to company: UserId={UserId}, CompanyId={CompanyId}",
                    request.InvitedByUserId,
                    invitation.CompanyId
                );
                return new ApiResponse<InvitationDTO>(
                    false,
                    "User not found or insufficient permissions",
                    null!
                );
            }

            if (!invitingUser.IsActive)
            {
                _logger.LogWarning(
                    "Inactive user trying to send invitation: UserId={UserId}",
                    request.InvitedByUserId
                );
                return new ApiResponse<InvitationDTO>(false, "User account is inactive", null!);
            }

            // 2. Verificar información de la company y límites
            var companyLimitQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                where c.Id == invitation.CompanyId
                select new
                {
                    CompanyId = c.Id,
                    c.CompanyName,
                    c.FullName,
                    c.Domain,
                    c.IsCompany,
                    CustomPlanId = cp.Id,
                    CustomPlanIsActive = cp.IsActive,
                    UserLimit = cp.UserLimit,
                    CurrentActiveUsers = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == invitation.CompanyId && u.IsActive
                    ),
                    PendingInvitations = _dbContext.Invitations.Count(i =>
                        i.CompanyId == invitation.CompanyId
                        && i.Status == InvitationStatus.Pending
                        && i.ExpiresAt > DateTime.UtcNow
                    ),
                };

            var companyLimits = await companyLimitQuery.FirstOrDefaultAsync(cancellationToken);
            if (companyLimits == null)
            {
                _logger.LogWarning("Company not found: {CompanyId}", invitation.CompanyId);
                return new ApiResponse<InvitationDTO>(false, "Company not found", null!);
            }

            if (!companyLimits.CustomPlanIsActive)
            {
                _logger.LogWarning("Company plan is inactive: {CompanyId}", invitation.CompanyId);
                return new ApiResponse<InvitationDTO>(false, "Company plan is inactive", null!);
            }

            // 3. VALIDAR LÍMITES: current users + pending invitations no debe exceder user limit
            var totalCommittedSlots =
                companyLimits.CurrentActiveUsers + companyLimits.PendingInvitations;
            if (totalCommittedSlots >= companyLimits.UserLimit)
            {
                _logger.LogWarning(
                    "User limit exceeded for company: {CompanyId}. "
                        + "Active users: {ActiveUsers}, Pending invitations: {PendingInvitations}, Limit: {UserLimit}",
                    invitation.CompanyId,
                    companyLimits.CurrentActiveUsers,
                    companyLimits.PendingInvitations,
                    companyLimits.UserLimit
                );
                return new ApiResponse<InvitationDTO>(
                    false,
                    $"User limit exceeded. Active users: {companyLimits.CurrentActiveUsers}, "
                        + $"Pending invitations: {companyLimits.PendingInvitations}, Limit: {companyLimits.UserLimit}",
                    null!
                );
            }

            // 4. Verificar que el email no esté ya registrado
            var emailExists = await _dbContext.TaxUsers.AnyAsync(
                u => u.Email == invitation.Email,
                cancellationToken
            );

            if (emailExists)
            {
                _logger.LogWarning("Email already registered: {Email}", invitation.Email);
                return new ApiResponse<InvitationDTO>(
                    false,
                    "Email already registered in the system",
                    null!
                );
            }

            // 5. Verificar que no exista una invitación pendiente para este email en esta company
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
                return new ApiResponse<InvitationDTO>(
                    false,
                    "A pending invitation already exists for this email",
                    null!
                );
            }

            // 6. Validar roles si se especifican
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
                        "Invalid role IDs in invitation: {RoleIds}",
                        string.Join(", ", invitation.RoleIds)
                    );
                    return new ApiResponse<InvitationDTO>(
                        false,
                        "Some specified roles are invalid or not allowed",
                        null!
                    );
                }
            }

            // 7. Generar token de invitación
            var (token, expiration) = _invitationTokenService.GenerateInvitation(
                invitation.CompanyId,
                invitation.Email,
                invitation.RoleIds
            );

            // 8. Construir link de invitación
            var invitationLink = _linkBuilder.BuildInvitationLink(request.Origin, token);

            // 9. Crear registro de invitación
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

            _logger.LogInformation(
                "Invitation created successfully: Id={InvitationId}, Email={Email}, CompanyId={CompanyId}, "
                    + "InvitedBy={InvitedByUserId}, Expires={ExpiresAt}",
                invitationEntity.Id,
                invitation.Email,
                invitation.CompanyId,
                request.InvitedByUserId,
                expiration
            );

            // 10. Publicar evento para envío de email
            _eventBus.Publish(
                new UserInvitationSentEvent(
                    Id: Guid.NewGuid(),
                    OccurredOn: DateTime.UtcNow,
                    CompanyId: invitation.CompanyId,
                    Email: invitation.Email,
                    InvitationLink: invitationLink,
                    ExpiresAt: expiration,
                    CompanyName: companyLimits.CompanyName,
                    CompanyFullName: companyLimits.FullName,
                    CompanyDomain: companyLimits.Domain,
                    IsCompany: companyLimits.IsCompany,
                    PersonalMessage: invitation.PersonalMessage
                )
            );

            // 11. Preparar DTO de respuesta con información completa
            var responseDto = new InvitationDTO
            {
                Id = invitationEntity.Id,
                CompanyId = invitation.CompanyId,
                InvitedByUserId = request.InvitedByUserId,
                Email = invitation.Email,
                Token = token,
                ExpiresAt = expiration,
                Status = InvitationStatus.Pending,
                PersonalMessage = invitation.PersonalMessage,
                RoleIds = invitation.RoleIds ?? new List<Guid>(),
                CreatedAt = invitationEntity.CreatedAt,
                InvitationLink = invitationLink,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,

                // Información del usuario que invitó
                InvitedByUserName = invitingUser.Name ?? string.Empty,
                InvitedByUserLastName = invitingUser.LastName ?? string.Empty,
                InvitedByUserEmail = invitingUser.Email,
                InvitedByUserIsOwner = invitingUser.IsOwner,

                // Información de la company
                CompanyName = companyLimits.CompanyName,
                CompanyFullName = companyLimits.FullName,
                CompanyDomain = companyLimits.Domain,
                CompanyIsCompany = companyLimits.IsCompany,

                // RoleNames se pueden agregar aquí si es necesario
                RoleNames = new List<string>(), // Se puede poblar con otra consulta si se necesita
            };

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
            return new ApiResponse<InvitationDTO>(false, ex.Message, null!);
        }
    }
}
