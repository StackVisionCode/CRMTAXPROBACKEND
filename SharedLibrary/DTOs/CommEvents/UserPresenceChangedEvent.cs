namespace SharedLibrary.DTOs.CommEvents;

public sealed record UserPresenceChangedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid UserId,
    bool IsOnline
) : IntegrationEvent(Id, OccurredOn);
