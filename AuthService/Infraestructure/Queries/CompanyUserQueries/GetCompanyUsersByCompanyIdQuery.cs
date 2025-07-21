using AuthService.DTOs.CompanyUserDTOs;
using Common;
using MediatR;

namespace Queries.CompanyUserQueries;

public record class GetCompanyUsersByCompanyIdQuery(Guid CompanyId)
    : IRequest<ApiResponse<List<CompanyUserGetDTO>>>;
