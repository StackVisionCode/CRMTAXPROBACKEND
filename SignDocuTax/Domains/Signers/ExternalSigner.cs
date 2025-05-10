using Common;
using Domain.Documents;
using Domain.Signatures;
using Domains.Contacts;
using Domains.Requirements;


namespace Domains.Signers;

public class ExternalSigner : BaseEntity
{
    public int DocumentId { get; set; }
    public Document? Document { get; set; }
    
    // Información del firmante externo
    public int ContactId { get; set; }

    public Contact Contact { get; set; }
   
    
    // Estado de la firma
    public int SignatureStatusId { get; set; }
    public SignatureStatus? SignatureStatus { get; set; }
    
    // Token único para identificación
  public string SigningToken { get; set; } = Guid.NewGuid().ToString();
    
    // Fechas
    public DateTime InvitationSentDate { get; set; }
    public DateTime? SignedDate { get; set; }
     public int RequirementSignatureId { get; set; }
    public RequirementSignature RequirementSignature { get; set; } = null!;
    

}