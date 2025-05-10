
using CompanyService.Application.Commons;
using CompanyService.Application.DTOs;
using MediatR;

namespace CompanyService.Infraestructure.Commands;   

    public record class UpdateCompanyCommands(CompanyDto CompanyDto) : IRequest<ApiResponse<bool>>;
    