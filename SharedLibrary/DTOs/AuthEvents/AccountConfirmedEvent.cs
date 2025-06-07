namespace SharedLibrary.DTOs;

public sealed record AccountConfirmedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid UserId,
    string Email,
    string DisplayName
) : IntegrationEvent(Id, OccurredOn);