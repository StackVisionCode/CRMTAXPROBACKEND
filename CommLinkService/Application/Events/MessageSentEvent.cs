using CommLinkService.Domain.Entities;
using SharedLibrary.DTOs;

namespace CommLinkService.Application.Events;

public sealed record MessageSentEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid MessageId,
    Guid RoomId,
    Guid SenderId,
    string Content,
    MessageType MessageType
) : IntegrationEvent(Id, OccurredOn);
