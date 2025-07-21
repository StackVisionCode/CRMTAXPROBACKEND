using AuthService.DTOs.CompanyUserDTOs;
using Common;
using MediatR;

namespace Queries.CompanyUserQueries;

public record class GetCompanyUserProfileQuery(Guid CompanyUserId)
    : IRequest<ApiResponse<CompanyUserProfileDTO>>;
