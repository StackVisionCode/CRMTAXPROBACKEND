using Common;
using MediatR;

namespace AuthService.Commands.CompanyUserCommands;

public record class CompanyAccountConfirmCommands(string Email, string Token)
    : IRequest<ApiResponse<Unit>>;
