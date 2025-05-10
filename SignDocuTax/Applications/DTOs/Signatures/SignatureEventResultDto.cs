namespace DTOs.Signatures;

public record SignatureEventResultDto(
    int Id,
    int DocumentId,
    int? TaxUserId,
    int? ExternalSignerId,
    string SignerName,
    string SignerEmail,
    DateTime SignatureDate,
    string SignatureType,
    string? SignatureImageUrl,
    string? DigitalSignatureHash,
    bool IsValid,
    string DocumentHashAtSigning,
    bool IsFullySigned);