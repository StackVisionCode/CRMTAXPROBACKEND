using CommLinkService.Domain.Entities;
using MediatR;

namespace CommLinkService.Infrastructure.Queries;

public sealed record GetRoomParticipantsQuery(Guid RoomId) : IRequest<GetRoomParticipantsResult>;

public sealed record GetRoomParticipantsResult(List<ParticipantDetailDto> Participants);

public sealed record ParticipantDetailDto(
    Guid UserId,
    string DisplayName,
    ParticipantRole Role,
    bool IsOnline,
    bool IsMuted,
    bool IsVideoEnabled,
    DateTime JoinedAt
);
