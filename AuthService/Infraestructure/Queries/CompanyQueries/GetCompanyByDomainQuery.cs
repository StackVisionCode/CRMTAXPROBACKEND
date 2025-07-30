using AuthService.Applications.DTOs.CompanyDTOs;
using Common;
using MediatR;

namespace Queries.CompanyQueries;

public record GetCompanyByDomainQuery(string Domain) : IRequest<ApiResponse<CompanyDTO>>;
