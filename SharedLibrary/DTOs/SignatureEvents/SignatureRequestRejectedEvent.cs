namespace SharedLibrary.DTOs.SignatureEvents;

public sealed record SignatureRequestRejectedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid SignatureRequestId,
    Guid DocumentId,
    Guid SignerId,
    string SignerEmail,
    string? Reason
) : IntegrationEvent(Id, OccurredOn);
