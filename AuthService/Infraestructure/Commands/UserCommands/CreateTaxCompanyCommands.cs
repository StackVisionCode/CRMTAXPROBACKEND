
using AuthService.Applications.DTOs.CompanyDTOs;
using Common;
using MediatR;

namespace Commands.UserCommands;

public record class CreateTaxCompanyCommands(NewCompanyDTO Companytax): IRequest<ApiResponse<bool>>;