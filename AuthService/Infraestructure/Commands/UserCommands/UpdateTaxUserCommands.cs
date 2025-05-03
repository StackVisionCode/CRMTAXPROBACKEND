using AuthService.DTOs.UserDTOs;
using Common;
using MediatR;
using UserDTOS;

namespace Commands.UserCommands;

public record class UpdateTaxUserCommands(UpdateUserDTO Usertax) : IRequest<ApiResponse<bool>>;