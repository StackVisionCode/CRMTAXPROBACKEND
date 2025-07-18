using MediatR;

namespace CommLinkService.Application.Commands;

public sealed record LeaveRoomCommand(Guid RoomId, Guid UserId) : IRequest<LeaveRoomResult>;

public sealed record LeaveRoomResult(bool Success);
