using AuthService.Applications.DTOs.CompanyDTOs;
using Common;
using MediatR;

namespace Queries.CompanyQueries;

public record GetAllCompaniesQuery : IRequest<ApiResponse<List<CompanyDTO>>>;
