namespace SharedLibrary.DTOs.SignatureEvents;

public sealed record DocumentPartiallySignedEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid SignatureRequestId,
    Guid DocumentId,
    Guid SignerId,
    string SignerEmail,
    string SignatureImageBase64,
    float PosX,
    float PosY,
    int PageNumber,
    string? FullName = null
) : IntegrationEvent(Id, OccurredOn);
