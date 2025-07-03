namespace SharedLibrary.DTOs.SignatureEvents;

public sealed record DocumentReadyToSealEvent(
    Guid Id,
    DateTime OccurredOn,
    Guid SignatureRequestId,
    Guid DocumentId,
    IReadOnlyList<SignedImageDto> Signatures // ← TODA la info necesaria
) : IntegrationEvent(Id, OccurredOn);

public sealed record SignedImageDto(
    Guid SignerId,
    string SignerEmail,
    int Page,
    float PosX,
    float PosY,
    float Width,
    float Height,
    string ImageBase64,
    string Thumbprint, // del cert. personal
    DateTime SignedAtUtc,
    string ClientIp,
    string UserAgent,
    DateTime ConsentAgreedAtUtc
);
