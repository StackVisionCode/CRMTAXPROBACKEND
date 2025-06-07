namespace SharedLibrary.DTOs;

public sealed record PasswordResetLinkEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid UserId,
    string Email,
    string DisplayName,
    string ResetLink,
    DateTime ExpiresAt
) : IntegrationEvent(Id, OccurredOn);
