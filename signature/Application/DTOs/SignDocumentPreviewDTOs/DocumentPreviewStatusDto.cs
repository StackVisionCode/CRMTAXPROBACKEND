namespace Application.DTOs;

public class DocumentPreviewStatusDto
{
    public bool CanPreview { get; set; }
    public bool IsExpired { get; set; }
    public bool AccessExpired { get; set; }
    public int RemainingAccess { get; set; }
    public string ExpiresAt { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
}
