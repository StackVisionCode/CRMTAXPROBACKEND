using MediatR;

namespace CommLinkService.Infrastructure.Commands;

public sealed record StartVideoCallCommand(Guid RoomId, Guid InitiatorId, List<Guid> ParticipantIds)
    : IRequest<StartVideoCallResult>;

public sealed record StartVideoCallResult(
    Guid CallId,
    string SignalServer,
    Dictionary<string, object> IceServers
);
