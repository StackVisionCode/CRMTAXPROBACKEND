using AuthService.DTOs.CompanyUserDTOs;
using Common;
using MediatR;

namespace Queries.CompanyUserQueries;

public record class GetCompanyUserByIdQuery(Guid CompanyUserId)
    : IRequest<ApiResponse<CompanyUserGetDTO>>;
