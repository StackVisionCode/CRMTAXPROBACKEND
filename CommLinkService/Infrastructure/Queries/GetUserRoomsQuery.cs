using CommLinkService.Domain.Entities;
using MediatR;

namespace CommLinkService.Infrastructure.Queries;

public sealed record GetUserRoomsQuery(Guid UserId) : IRequest<GetUserRoomsResult>;

public sealed record GetUserRoomsResult(List<RoomDto> Rooms);

public sealed record RoomDto(
    Guid Id,
    string Name,
    RoomType Type,
    DateTime LastActivityAt,
    int UnreadCount,
    List<ParticipantDto> Participants
);

public sealed record ParticipantDto(
    Guid UserId,
    string DisplayName,
    bool IsOnline,
    ParticipantRole Role
);
