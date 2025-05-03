
using AuthService.DTOs.UserDTOs;
using Common;
using MediatR;
using UserDTOS;

namespace Commands.UserCommands;

public record  class CreateTaxUserCommands(NewUserDTO Usertax): IRequest<ApiResponse<bool>>;