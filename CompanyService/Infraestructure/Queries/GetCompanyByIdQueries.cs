using MediatR;
using CompanyService.Application.Commons;
using CompanyService.Application.DTOs;
namespace CompanyService.Infraestructure.Queries;

public record class GetCompanyByIdQueries(int Id): IRequest<ApiResponse<CompanyDto>>;