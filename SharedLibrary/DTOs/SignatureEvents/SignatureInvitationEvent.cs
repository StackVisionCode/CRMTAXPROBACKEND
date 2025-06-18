namespace SharedLibrary.DTOs.SignatureEvents;

public sealed record SignatureInvitationEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid SignerId,
    string SignerEmail,
    string ConfirmLink,
    DateTime ExpiresAt
) : IntegrationEvent(Id, OccurredOn);
