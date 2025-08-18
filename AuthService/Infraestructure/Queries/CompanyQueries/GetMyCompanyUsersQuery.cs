using AuthService.DTOs.CompanyDTOs;
using Common;
using MediatR;

namespace Queries.CompanyQueries;

public record GetMyCompanyUsersQuery(Guid CompanyId)
    : IRequest<ApiResponse<CompanyUsersCompleteDTO>>;
