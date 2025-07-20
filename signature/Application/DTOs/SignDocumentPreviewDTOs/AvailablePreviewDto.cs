namespace Application.DTOs;

public class AvailablePreviewDto
{
    public bool HasPreview { get; set; }
    public string? PreviewUrl { get; set; }
    public string? AccessToken { get; set; }
    public string? SessionId { get; set; }
    public string? ExpiresAt { get; set; }
    public string Message { get; set; } = string.Empty;
}
