using AuthService.DTOs.CompanyUserDTOs;
using Common;
using MediatR;

namespace Commands.CompanyUserCommands;

public record class UpdateCompanyUserCommand(UpdateCompanyUserDTO CompanyUser)
    : IRequest<ApiResponse<bool>>;
