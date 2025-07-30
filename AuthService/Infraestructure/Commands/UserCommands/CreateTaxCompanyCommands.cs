using AuthService.Applications.DTOs.CompanyDTOs;
using Common;
using MediatR;

namespace Commands.UserCommands;

public record CreateTaxCompanyCommands(NewCompanyDTO CompanyTax, string Origin)
    : IRequest<ApiResponse<bool>>;
