namespace SharedLibrary.DTOs.SignatureEvents;

public sealed record SignatureRequestRejectedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid SignatureRequestId,
    Guid DocumentId,
    Guid RejectedBySignerId,
    string RejectedByEmail,
    string? RejectedByFullName,
    Guid RecipientSignerId,
    string RecipientEmail,
    string? RecipientFullName,
    string? Reason,
    DateTime RejectedAtUtc
) : IntegrationEvent(Id, OccurredOn);
