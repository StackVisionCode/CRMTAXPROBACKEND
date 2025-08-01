using MediatR;

namespace CommLinkService.Infrastructure.Commands;

public sealed record LeaveRoomCommand(Guid RoomId, Guid UserId) : IRequest<LeaveRoomResult>;

public sealed record LeaveRoomResult(bool Success);
