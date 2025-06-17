namespace Domain.Entities;

public class Signature
{

 public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string FilePath { get; set; } = null!;
    public string? Base64Image { get; set; }

    public SignatureStatus Status { get; set; }
    public SignatureType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SignedAt { get; set; }
    public string CertificateThumbprint { get; set; } = default!;

}


 