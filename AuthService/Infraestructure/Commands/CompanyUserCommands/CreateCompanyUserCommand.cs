using AuthService.DTOs.CompanyUserDTOs;
using Common;
using MediatR;

namespace Commands.CompanyUserCommands;

public record class CreateCompanyUserCommand(NewCompanyUserDTO CompanyUser, string Origin)
    : IRequest<ApiResponse<bool>>;
