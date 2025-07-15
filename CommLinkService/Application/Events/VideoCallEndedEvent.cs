using SharedLibrary.DTOs;

namespace CommLinkService.Application.Events;

public sealed record VideoCallEndedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid CallId,
    Guid RoomId,
    Guid EndedBy,
    DateTime EndedAt
) : IntegrationEvent(Id, OccurredOn);
