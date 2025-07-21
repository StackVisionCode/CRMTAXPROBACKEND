using Common;
using MediatR;

namespace Commands.SessionCommands;

public record class CompanyUserLogoutAllCommand(Guid CompanyUserId) : IRequest<ApiResponse<bool>>;
