namespace Application.DTOs;

public class DocumentPreviewAccessDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public Guid SealedDocumentId { get; set; }
    public Guid SignerId { get; set; }
    public string SignerEmail { get; set; } = string.Empty;
    public string ExpiresAt { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public bool CanAccess { get; set; }
    public int AccessCount { get; set; }
    public int MaxAccessCount { get; set; }
    public bool IsActive { get; set; }
    public string? LastAccessedAt { get; set; }
}
