using Domain;

namespace Application.DTOs;

public class CreateSignatureDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string? Base64Image { get; set; }
    public SignatureType Type { get; set; }
   

}



