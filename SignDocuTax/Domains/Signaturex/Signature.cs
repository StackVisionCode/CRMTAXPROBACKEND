namespace Signaturex;

public class Signature
{
      public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FilePath { get; set; } = default!;
    public string InitialsPath { get; set; } = default!;
    public string PreviewPath { get; set; } = default!;
    public SignatureType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CertificateThumbprint { get; set; } = default!;
}