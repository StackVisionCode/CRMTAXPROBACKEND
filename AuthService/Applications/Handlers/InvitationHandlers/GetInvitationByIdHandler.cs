using AuthService.DTOs.InvitationDTOs;
using Common;
using Infraestructure.Context;
using Infraestructure.Queries.InvitationsQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.InvitationHandlers;

/// <summary>
/// Handler para obtener invitación por ID
/// </summary>
public class GetInvitationByIdHandler
    : IRequestHandler<GetInvitationByIdQuery, ApiResponse<InvitationDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetInvitationByIdHandler> _logger;

    public GetInvitationByIdHandler(
        ApplicationDbContext dbContext,
        ILogger<GetInvitationByIdHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<InvitationDTO>> Handle(
        GetInvitationByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var invitationQuery =
                from i in _dbContext.Invitations
                join c in _dbContext.Companies on i.CompanyId equals c.Id
                join invitedBy in _dbContext.TaxUsers on i.InvitedByUserId equals invitedBy.Id
                join cancelledBy in _dbContext.TaxUsers
                    on i.CancelledByUserId equals cancelledBy.Id
                    into cancelledByGroup
                from cancelledBy in cancelledByGroup.DefaultIfEmpty()
                join registeredUser in _dbContext.TaxUsers
                    on i.RegisteredUserId equals registeredUser.Id
                    into registeredGroup
                from registeredUser in registeredGroup.DefaultIfEmpty()
                where i.Id == request.InvitationId
                select new InvitationDTO
                {
                    Id = i.Id,
                    CompanyId = i.CompanyId,
                    InvitedByUserId = i.InvitedByUserId,
                    Email = i.Email,
                    Token = i.Token,
                    ExpiresAt = i.ExpiresAt,
                    Status = i.Status,
                    PersonalMessage = i.PersonalMessage,
                    RoleIds = i.RoleIds,
                    CreatedAt = i.CreatedAt,
                    AcceptedAt = i.AcceptedAt,
                    CancelledAt = i.CancelledAt,
                    CancelledByUserId = i.CancelledByUserId,
                    CancellationReason = i.CancellationReason,
                    RegisteredUserId = i.RegisteredUserId,
                    InvitationLink = i.InvitationLink,
                    IpAddress = i.IpAddress,
                    UserAgent = i.UserAgent,

                    InvitedByUserName = invitedBy.Name ?? string.Empty,
                    InvitedByUserLastName = invitedBy.LastName ?? string.Empty,
                    InvitedByUserEmail = invitedBy.Email,
                    InvitedByUserIsOwner = invitedBy.IsOwner,

                    CancelledByUserName = cancelledBy != null ? cancelledBy.Name : null,
                    CancelledByUserLastName = cancelledBy != null ? cancelledBy.LastName : null,
                    CancelledByUserEmail = cancelledBy != null ? cancelledBy.Email : null,

                    RegisteredUserName = registeredUser != null ? registeredUser.Name : null,
                    RegisteredUserLastName =
                        registeredUser != null ? registeredUser.LastName : null,
                    RegisteredUserEmail = registeredUser != null ? registeredUser.Email : null,

                    CompanyName = c.CompanyName,
                    CompanyFullName = c.FullName,
                    CompanyDomain = c.Domain,
                    CompanyIsCompany = c.IsCompany,

                    RoleNames = new List<string>(), // Se puede poblar si es necesario
                };

            var invitation = await invitationQuery.FirstOrDefaultAsync(cancellationToken);

            if (invitation == null)
            {
                _logger.LogWarning("Invitation not found: {InvitationId}", request.InvitationId);
                return new ApiResponse<InvitationDTO>(false, "Invitation not found", null!);
            }

            // Validación de permisos si se especifica RequestingUserId
            if (request.RequestingUserId.HasValue)
            {
                var hasAccess = await _dbContext.TaxUsers.AnyAsync(
                    u =>
                        u.Id == request.RequestingUserId.Value
                        && u.CompanyId == invitation.CompanyId
                        && u.IsActive,
                    cancellationToken
                );

                if (!hasAccess)
                {
                    _logger.LogWarning(
                        "User {RequestingUserId} doesn't have access to invitation {InvitationId}",
                        request.RequestingUserId,
                        request.InvitationId
                    );
                    return new ApiResponse<InvitationDTO>(false, "Access denied", null!);
                }
            }

            return new ApiResponse<InvitationDTO>(
                true,
                "Invitation retrieved successfully",
                invitation
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving invitation by ID: {InvitationId}",
                request.InvitationId
            );
            return new ApiResponse<InvitationDTO>(false, ex.Message, null!);
        }
    }
}
