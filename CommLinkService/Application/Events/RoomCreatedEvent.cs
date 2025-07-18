using CommLinkService.Domain.Entities;
using SharedLibrary.DTOs;

namespace CommLinkService.Application.Events;

public sealed record RoomCreatedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid RoomId,
    string RoomName,
    RoomType RoomType,
    Guid CreatedBy,
    List<Guid> ParticipantIds
) : IntegrationEvent(Id, OccurredOn);
