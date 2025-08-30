using AuthService.DTOs.InvitationDTOs;
using Common;
using MediatR;

namespace Infraestructure.Queries.InvitationsQueries;

/// <summary>
/// Query para obtener todas las invitaciones (para Developer/Admin global)
/// </summary>
public record GetAllInvitationsQuery(
    int Page = 1,
    int PageSize = 10,
    Guid? CompanyIdFilter = null,
    InvitationStatus? StatusFilter = null,
    string? EmailFilter = null,
    DateTime? DateFromFilter = null,
    DateTime? DateToFilter = null,
    bool IncludeExpired = true
) : IRequest<ApiResponse<PagedResult<InvitationDTO>>>;
