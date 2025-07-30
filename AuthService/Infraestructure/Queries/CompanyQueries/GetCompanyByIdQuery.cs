using AuthService.Applications.DTOs.CompanyDTOs;
using Common;
using MediatR;

namespace Queries.CompanyQueries;

public record GetCompanyByIdQuery(Guid Id) : IRequest<ApiResponse<CompanyDTO>>;
