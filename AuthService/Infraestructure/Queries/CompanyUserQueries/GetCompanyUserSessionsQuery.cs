using AuthService.DTOs.CompanyUserSessionDTOs;
using Common;
using MediatR;

namespace Queries.CompanyUserQueries;

public record class GetCompanyUserSessionsQuery(Guid CompanyUserId)
    : IRequest<ApiResponse<List<ReadCompanyUserSessionDTO>>>;
