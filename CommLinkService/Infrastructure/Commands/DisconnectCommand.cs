using Common;
using MediatR;

namespace CommLinkService.Application.Commands;

public record class DisconnectCommand(string ConnectionId) : IRequest<ApiResponse<bool>>;
