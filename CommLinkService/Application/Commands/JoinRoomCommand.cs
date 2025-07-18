using CommLinkService.Domain.Entities;
using MediatR;

namespace CommLinkService.Application.Commands;

public sealed record JoinRoomCommand(Guid RoomId, Guid UserId, string ConnectionId)
    : IRequest<JoinRoomResult>;

public sealed record JoinRoomResult(bool Success, string? ErrorMessage, ParticipantRole? Role);
