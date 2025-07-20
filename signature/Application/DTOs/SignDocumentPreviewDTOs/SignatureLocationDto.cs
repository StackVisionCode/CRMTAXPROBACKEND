namespace Application.DTOs;

public class SignatureLocationDto
{
    public Guid SignerId { get; set; }
    public string SignerName { get; set; } = string.Empty;
    public string SignerEmail { get; set; } = string.Empty;
    public int Page { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public string SignedAtUtc { get; set; } = string.Empty;
    public string SignatureType { get; set; } = string.Empty;
    public bool IsCurrentSigner { get; set; }
}
