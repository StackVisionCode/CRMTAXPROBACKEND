namespace Application.DTOs;

public class SignerPreviewDto
{
    public Guid SignerId { get; set; }
    public string SignerName { get; set; } = string.Empty;
    public string SignerEmail { get; set; } = string.Empty;
    public int Order { get; set; }
    public SignerStatus Status { get; set; }
    public string? SignedAtUtc { get; set; }
    public bool IsCurrentSigner { get; set; }
}
