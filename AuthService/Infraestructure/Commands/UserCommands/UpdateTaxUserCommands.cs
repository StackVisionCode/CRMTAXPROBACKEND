using AuthService.DTOs.UserDTOs;
using Common;
using MediatR;

namespace Commands.UserCommands;

public record class UpdateTaxUserCommands(UpdateUserDTO UserTax) : IRequest<ApiResponse<bool>>;
