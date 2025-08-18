using AuthService.Applications.DTOs.CompanyDTOs;
using AuthService.DTOs.UserDTOs;
using Common;
using MediatR;

namespace Commands.UserCommands;

public record CreateUserByDeveloperCommand(CreateUserByDeveloperDTO UserData, string Origin)
    : IRequest<ApiResponse<UserGetDTO>>;
