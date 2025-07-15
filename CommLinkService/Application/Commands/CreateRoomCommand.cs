using CommLinkService.Domain.Entities;
using MediatR;

namespace CommLinkService.Application.Commands;

public sealed record CreateRoomCommand(
    string Name,
    RoomType Type,
    Guid CreatorId,
    List<Guid>? ParticipantIds = null,
    int MaxParticipants = 10
) : IRequest<CreateRoomResult>;

public sealed record CreateRoomResult(Guid RoomId, string Name, RoomType Type, DateTime CreatedAt);
