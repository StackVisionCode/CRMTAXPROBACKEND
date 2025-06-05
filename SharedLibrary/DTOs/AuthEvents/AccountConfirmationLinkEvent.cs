namespace SharedLibrary.DTOs.AuthEvents;

public sealed record AccountConfirmationLinkEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid UserId,
    string Email,
    string DisplayName,
    string ConfirmLink,
    DateTime ExpiresAt
) : IntegrationEvent(Id, OccurredOn);