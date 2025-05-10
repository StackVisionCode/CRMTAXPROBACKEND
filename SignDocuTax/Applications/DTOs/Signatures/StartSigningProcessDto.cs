namespace DTOs.Signatures;

public record StartSigningProcessDto(
    int DocumentId,
    List<int>? InternalSigners,
    List<int> ExternalSigners,
    DateTime? ExpiryDate,
    string? CustomMessage);