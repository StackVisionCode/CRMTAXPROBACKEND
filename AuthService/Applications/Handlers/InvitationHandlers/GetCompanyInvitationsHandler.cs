using AuthService.DTOs.InvitationDTOs;
using Common;
using Infraestructure.Context;
using Infraestructure.Queries.InvitationsQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.InvitationHandlers;

/// <summary>
/// Handler para obtener invitaciones de una company específica
/// </summary>
public class GetCompanyInvitationsHandler
    : IRequestHandler<GetCompanyInvitationsQuery, ApiResponse<PagedResult<InvitationDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetCompanyInvitationsHandler> _logger;

    public GetCompanyInvitationsHandler(
        ApplicationDbContext dbContext,
        ILogger<GetCompanyInvitationsHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<InvitationDTO>>> Handle(
        GetCompanyInvitationsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Query base con joins optimizados
            var baseQuery =
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
                where i.CompanyId == request.CompanyId
                select new
                {
                    // Invitation data
                    i.Id,
                    i.CompanyId,
                    i.InvitedByUserId,
                    i.Email,
                    i.Token,
                    i.ExpiresAt,
                    i.Status,
                    i.PersonalMessage,
                    i.RoleIds,
                    i.CreatedAt,
                    i.AcceptedAt,
                    i.CancelledAt,
                    i.CancelledByUserId,
                    i.CancellationReason,
                    i.RegisteredUserId,
                    i.InvitationLink,
                    i.IpAddress,
                    i.UserAgent,

                    // InvitedBy user data
                    InvitedByUserName = invitedBy.Name ?? string.Empty,
                    InvitedByUserLastName = invitedBy.LastName ?? string.Empty,
                    InvitedByUserEmail = invitedBy.Email,
                    InvitedByUserIsOwner = invitedBy.IsOwner,

                    // CancelledBy user data (optional)
                    CancelledByUserName = cancelledBy != null ? cancelledBy.Name : null,
                    CancelledByUserLastName = cancelledBy != null ? cancelledBy.LastName : null,
                    CancelledByUserEmail = cancelledBy != null ? cancelledBy.Email : null,

                    // RegisteredUser data (optional)
                    RegisteredUserName = registeredUser != null ? registeredUser.Name : null,
                    RegisteredUserLastName = registeredUser != null
                        ? registeredUser.LastName
                        : null,
                    RegisteredUserEmail = registeredUser != null ? registeredUser.Email : null,

                    // Company data
                    CompanyName = c.CompanyName,
                    CompanyFullName = c.FullName,
                    CompanyDomain = c.Domain,
                    CompanyIsCompany = c.IsCompany,
                };

            // Aplicar filtros
            if (request.StatusFilter.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.Status == request.StatusFilter.Value);
            }

            if (!string.IsNullOrEmpty(request.EmailFilter))
            {
                baseQuery = baseQuery.Where(x => x.Email.Contains(request.EmailFilter));
            }

            if (request.InvitedByUserIdFilter.HasValue)
            {
                baseQuery = baseQuery.Where(x =>
                    x.InvitedByUserId == request.InvitedByUserIdFilter.Value
                );
            }

            if (request.DateFromFilter.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.CreatedAt >= request.DateFromFilter.Value);
            }

            if (request.DateToFilter.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.CreatedAt <= request.DateToFilter.Value);
            }

            if (!request.IncludeExpired)
            {
                var now = DateTime.UtcNow;
                baseQuery = baseQuery.Where(x =>
                    x.Status != InvitationStatus.Expired
                    && (x.Status != InvitationStatus.Pending || x.ExpiresAt > now)
                );
            }

            // Contar total
            var totalCount = await baseQuery.CountAsync(cancellationToken);

            // Aplicar paginación y ordenamiento
            var pagedData = await baseQuery
                .OrderByDescending(x => x.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Convertir a DTOs
            var invitationDtos = pagedData
                .Select(x => new InvitationDTO
                {
                    Id = x.Id,
                    CompanyId = x.CompanyId,
                    InvitedByUserId = x.InvitedByUserId,
                    Email = x.Email,
                    Token = x.Token,
                    ExpiresAt = x.ExpiresAt,
                    Status = x.Status,
                    PersonalMessage = x.PersonalMessage,
                    RoleIds = x.RoleIds,
                    CreatedAt = x.CreatedAt,
                    AcceptedAt = x.AcceptedAt,
                    CancelledAt = x.CancelledAt,
                    CancelledByUserId = x.CancelledByUserId,
                    CancellationReason = x.CancellationReason,
                    RegisteredUserId = x.RegisteredUserId,
                    InvitationLink = x.InvitationLink,
                    IpAddress = x.IpAddress,
                    UserAgent = x.UserAgent,

                    InvitedByUserName = x.InvitedByUserName,
                    InvitedByUserLastName = x.InvitedByUserLastName,
                    InvitedByUserEmail = x.InvitedByUserEmail,
                    InvitedByUserIsOwner = x.InvitedByUserIsOwner,

                    CancelledByUserName = x.CancelledByUserName,
                    CancelledByUserLastName = x.CancelledByUserLastName,
                    CancelledByUserEmail = x.CancelledByUserEmail,

                    RegisteredUserName = x.RegisteredUserName,
                    RegisteredUserLastName = x.RegisteredUserLastName,
                    RegisteredUserEmail = x.RegisteredUserEmail,

                    CompanyName = x.CompanyName,
                    CompanyFullName = x.CompanyFullName,
                    CompanyDomain = x.CompanyDomain,
                    CompanyIsCompany = x.CompanyIsCompany,

                    // RoleNames se pueden poblar si es necesario
                    RoleNames = new List<string>(),
                })
                .ToList();

            var pagedResult = new PagedResult<InvitationDTO>
            {
                Items = invitationDtos,
                TotalItems = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
            };

            _logger.LogDebug(
                "Retrieved {Count} invitations for company {CompanyId} (page {Page}/{TotalPages})",
                invitationDtos.Count,
                request.CompanyId,
                request.Page,
                pagedResult.TotalPages
            );

            return new ApiResponse<PagedResult<InvitationDTO>>(
                true,
                "Invitations retrieved successfully",
                pagedResult
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving company invitations for CompanyId: {CompanyId}",
                request.CompanyId
            );
            return new ApiResponse<PagedResult<InvitationDTO>>(
                false,
                ex.Message,
                new PagedResult<InvitationDTO>()
            );
        }
    }
}
