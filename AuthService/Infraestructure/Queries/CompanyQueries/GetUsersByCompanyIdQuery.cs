using AuthService.DTOs.UserDTOs;
using Common;
using MediatR;

namespace Queries.CompanyQueries;

public record GetUsersByCompanyIdQuery(Guid CompanyId) : IRequest<ApiResponse<List<UserGetDTO>>>;
