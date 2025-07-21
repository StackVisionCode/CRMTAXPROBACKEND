using Common;
using MediatR;

namespace Commands.SessionCommands;

public record class CompanyUserLogoutCommand(Guid SessionId, Guid CompanyUserId)
    : IRequest<ApiResponse<bool>>;
