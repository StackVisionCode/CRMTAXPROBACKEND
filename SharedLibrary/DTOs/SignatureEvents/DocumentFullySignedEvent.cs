namespace SharedLibrary.DTOs.SignatureEvents;

public sealed record DocumentFullySignedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid SignatureRequestId,
    Guid DocumentId
) : IntegrationEvent(Id, OccurredOn);
