namespace signature.Application.DTOs;

public class SignDocumentDto
{
    public required string Token { get; set; }
    public required string SignatureImageBase64 { get; set; }
    public required DigitalCertificateDto Certificate { get; set; }
    public required string ClientIp { get; set; } // NEW
    public required string UserAgent { get; set; } // NEW
    public DateTime SignedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ConsentAgreedAtUtc { get; set; } = DateTime.UtcNow;
}
