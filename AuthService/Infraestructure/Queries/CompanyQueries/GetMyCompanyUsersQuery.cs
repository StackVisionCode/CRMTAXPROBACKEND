using AuthService.DTOs.UserCompanyDTOs;
using Common;
using MediatR;

namespace Queries.CompanyQueries;

public record class GetMyCompanyUsersQuery(Guid CompanyId)
    : IRequest<ApiResponse<List<UserCompanyDTO>>>;
