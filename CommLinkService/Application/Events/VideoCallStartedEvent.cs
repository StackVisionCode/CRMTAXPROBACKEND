using SharedLibrary.DTOs;

namespace CommLinkService.Application.Events;

public sealed record VideoCallStartedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid CallId,
    Guid RoomId,
    Guid InitiatorId,
    List<Guid> ParticipantIds
) : IntegrationEvent(Id, OccurredOn);
