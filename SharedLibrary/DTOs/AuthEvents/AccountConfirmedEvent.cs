namespace SharedLibrary.DTOs;

public sealed record AccountConfirmedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid UserId,
    string Email,
    string DisplayName,
    bool IsCompany
) : IntegrationEvent(Id, OccurredOn);
