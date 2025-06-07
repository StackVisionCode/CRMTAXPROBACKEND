using AuthService.DTOs.UserDTOs;
using Common;
using MediatR;

namespace Commands.UserCommands;

public record class CreateTaxUserCommands(NewUserDTO Usertax, string Origin) : IRequest<ApiResponse<bool>>;
