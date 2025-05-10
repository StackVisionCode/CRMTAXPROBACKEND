using Common;
using Domains.Requirements;
namespace Domain.Documents;
public class Document : BaseEntity
{
    public required int CompanyId { get; set; }
    public required int TaxUserId { get; set; }
    public string? Name { get; set; }
    public required string FileName { get; set; }
    public int DocumentStatusId { get; set; }
    public DocumentStatus? DocumentStatus { get; set; }
    public int DocumentTypeId { get; set; }
    public string? OriginalHash { get; set; } // Hash SHA-256 al subir el documento  
    public string? SignedHash { get; set; }   // Hash después de firmar  
    public string? SignedDocumentPath { get; set; } // Ruta del PDF firmado  
    public bool IsSigned { get; set; } // Indica si el documento ha sido firmado
    public DocumentType? DocumentTypes { get; set; }
    public required string? Path { get; set; }
    public int RequirementSignatureId{ get; set; }
    
    public ICollection<RequirementSignature>? RequirementSignatures { get; set; }


}
