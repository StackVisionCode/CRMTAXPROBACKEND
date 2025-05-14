namespace DTOs.Signatures;

public record SigningProcessResultDto(
    int RequirementId,
    int DocumentId,
    string DocumentName,
    DateTime CreatedAt,
    DateTime ExpiryDate,
    int TotalSigners,
    int CompletedSignatures,
    List<SignerInfoDto> Signers);

public record SignerInfoDto(
    int Id,
    string Name,
    string Email,
    string Type, // "Internal" o "External"
    string Status,
    DateTime? SignedDate,
    string? SigningUrl = null);