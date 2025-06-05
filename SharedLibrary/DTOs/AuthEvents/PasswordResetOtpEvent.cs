namespace SharedLibrary.DTOs;

public sealed record PasswordResetOtpEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid UserId,
    string Email,
    string DisplayName,
    string Otp,
    DateTime ExpiresAt
) : IntegrationEvent(Id, OccurredOn);
