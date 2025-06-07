using Common;
using MediatR;

namespace AuthService.Commands.UserConfirmCommands;

public record class AccountConfirmCommands(string Email, string Token) : IRequest<ApiResponse<Unit>>;
