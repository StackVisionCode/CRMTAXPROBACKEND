using Signaturex;

namespace Applications.DTOs;
public class CreateSignatureDto
{
    public Guid UserId { get; set; }
    public string? FullName { get; set; }
    public string? Base64Image { get; set; }
    public SignatureType Type { get; set; }
}