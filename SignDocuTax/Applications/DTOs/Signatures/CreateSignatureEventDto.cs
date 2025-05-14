namespace DTOs.Signatures;

public record CreateSignatureEventDto(
    int DocumentId,
    int? TaxUserId,
    int? ExternalSignerId,
    string IpAddress,
    string DeviceInfo,
    string? SignatureImage,
    string? DigitalSignature);