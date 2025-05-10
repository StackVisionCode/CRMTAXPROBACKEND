using Common;
using Domains.Signatures;

namespace Domains.Firms;
public class Firm : BaseEntity
{
    public int CustomerId { get; set; }
    public int SignatureTypeId { get; set; }
    public SignatureType? SignatureType { get; set; }
    public int CompanyId { get; set; }
    public int TaxUserId { get; set; }
    public int FirmStatusId { get; set; }
    public FirmStatus? FirmStatus { get; set; }
    public string? Name { get; set; }
    public string Path { get; set; } = string.Empty;
     public string? CertificateThumbprint { get; set; } // Ej: "A5B8C3...SHA1"
    public DateTime CertificateExpiry { get; set; }


}
