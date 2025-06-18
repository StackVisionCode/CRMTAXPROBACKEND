namespace SharedLibrary.DTOs.SignatureEvents;

public sealed record DocumentPartiallySignedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid RequestId,
    Guid DocumentId,
    Guid SignerId
) : IntegrationEvent(Id, OccurredOn);
