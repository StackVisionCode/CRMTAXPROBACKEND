using AuthService.DTOs.CompanyUserSessionDTOs;
using Common;
using MediatR;

namespace Queries.CompanyUserQueries;

public record class GetActiveCompanyUserSessionsQuery(Guid CompanyUserId)
    : IRequest<ApiResponse<List<CompanyUserSessionDTO>>>;
