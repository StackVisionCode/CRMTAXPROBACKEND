namespace SharedLibrary.DTOs;

public sealed record PasswordChangedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid UserId,
    string Email,
    string DisplayName,
    DateTime ChangedAt
) : IntegrationEvent(Id, OccurredOn);
