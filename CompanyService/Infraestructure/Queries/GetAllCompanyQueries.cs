
using CompanyService.Application.Commons;
using CompanyService.Application.DTOs;
using MediatR;

namespace CompanyService.Infraestructure.Queries;

    public record class GetAllCompanyQueries : IRequest<ApiResponse<List<CompanyDto>>>;
