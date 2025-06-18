namespace SharedLibrary.DTOs.SignatureEvents;

public sealed record DocumentPartiallySignedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid SignatureRequestId,
    Guid DocumentId,
    Guid SignerId,
    string SignerEmail
) : IntegrationEvent(Id, OccurredOn);
