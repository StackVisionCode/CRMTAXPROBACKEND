namespace signature.Application.DTOs;

public class SignDocumentDto
{
    public required string Token { get; set; }
    public required string SignatureImageBase64 { get; set; }
    public required DigitalCertificateDto Certificate { get; set; }
}
