using AuthService.Applications.DTOs.CompanyDTOs;
using Common;
using MediatR;

namespace Commands.UserCommands;

public record class UpdateTaxCompanyCommands(UpdateCompanyDTO CompanyTax)
    : IRequest<ApiResponse<bool>>;
