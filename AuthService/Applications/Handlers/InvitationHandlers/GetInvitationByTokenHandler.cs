using AuthService.DTOs.InvitationDTOs;
using Common;
using Infraestructure.Context;
using Infraestructure.Queries.InvitationsQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.InvitationHandlers;

/// <summary>
/// Handler para obtener invitación por token
/// </summary>
public class GetInvitationByTokenHandler
    : IRequestHandler<GetInvitationByTokenQuery, ApiResponse<InvitationDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetInvitationByTokenHandler> _logger;

    public GetInvitationByTokenHandler(
        ApplicationDbContext dbContext,
        ILogger<GetInvitationByTokenHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<InvitationDTO>> Handle(
        GetInvitationByTokenQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var invitationQuery =
                from i in _dbContext.Invitations
                join c in _dbContext.Companies on i.CompanyId equals c.Id
                join invitedBy in _dbContext.TaxUsers on i.InvitedByUserId equals invitedBy.Id
                where i.Token == request.Token
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

                    InvitedByUserName = invitedBy.Name ?? string.Empty,
                    InvitedByUserLastName = invitedBy.LastName ?? string.Empty,
                    InvitedByUserEmail = invitedBy.Email,
                    InvitedByUserIsOwner = invitedBy.IsOwner,

                    CompanyName = c.CompanyName,
                    CompanyFullName = c.FullName,
                    CompanyDomain = c.Domain,
                    CompanyIsCompany = c.IsCompany,

                    RoleNames = new List<string>(),
                };

            var invitation = await invitationQuery.FirstOrDefaultAsync(cancellationToken);

            if (invitation == null)
            {
                _logger.LogWarning("Invitation not found for token: {Token}", request.Token);
                return new ApiResponse<InvitationDTO>(false, "Invalid invitation token", null!);
            }

            return new ApiResponse<InvitationDTO>(
                true,
                "Invitation retrieved successfully",
                invitation
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invitation by token");
            return new ApiResponse<InvitationDTO>(false, ex.Message, null!);
        }
    }
}
