namespace signature.Application.DTOs;

public class DigitalCertificateDto
{
    public required string Thumbprint { get; set; }
    public required string Subject { get; set; }
    public DateTime NotBefore { get; set; }
    public DateTime NotAfter { get; set; }
}
