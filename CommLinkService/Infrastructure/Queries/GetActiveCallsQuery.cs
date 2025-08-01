using MediatR;

namespace CommLinkService.Infrastructure.Queries;

public sealed record GetActiveCallsQuery(Guid UserId) : IRequest<GetActiveCallsResult>;

public sealed record GetActiveCallsResult(List<ActiveCallDto> Calls);

public sealed record ActiveCallDto(
    Guid CallId,
    Guid RoomId,
    string RoomName,
    DateTime StartedAt,
    List<Guid> ParticipantIds
);
