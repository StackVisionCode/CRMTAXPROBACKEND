using AuthService.DTOs.InvitationDTOs;
using Common;
using MediatR;

namespace Infraestructure.Queries.InvitationsQueries;

/// <summary>
/// Query para obtener invitaciones de una company específica
/// </summary>
public record GetCompanyInvitationsQuery(
    Guid CompanyId,
    int Page = 1,
    int PageSize = 10,
    InvitationStatus? StatusFilter = null,
    string? EmailFilter = null,
    Guid? InvitedByUserIdFilter = null,
    DateTime? DateFromFilter = null,
    DateTime? DateToFilter = null,
    bool IncludeExpired = true
) : IRequest<ApiResponse<PagedResult<InvitationDTO>>>;
