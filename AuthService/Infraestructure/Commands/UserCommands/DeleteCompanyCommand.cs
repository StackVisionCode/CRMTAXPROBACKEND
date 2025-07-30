using Common;
using MediatR;

namespace Commands.UserCommands;

public record DeleteCompanyCommand(Guid Id) : IRequest<ApiResponse<bool>>;
