using MediatR;
using CompanyService.Application.Commons;
using CompanyService.Application.DTOs;
namespace CompanyService.Infraestructure.Commands;

public record class CreateCompanyCommand(CompanyDto Companydto) : IRequest<ApiResponse<bool>>;