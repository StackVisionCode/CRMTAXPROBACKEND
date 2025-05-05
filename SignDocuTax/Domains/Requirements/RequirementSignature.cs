using Common;
using Domain.Documents;
using Domain.Signatures;
using Domains.Firms;

namespace Domains.Requirements;


public class RequirementSignature : BaseEntity
{

    public int CustomerId { get; set; }
    public int DocumentId { get; set; }
    public Document? Document { get; set; }
    public int TaxUserId { get; set; }
    public int CompanyId { get; set; }

    public int StatusSignatureId { get; set; }
    public StatusRequirement? StatusRequirement { get; set; }

    public int FirmId { get; set; }
    public ICollection<Firm>? Firm { get; set; }
    public int Quantity { get; set; } = 1;

    public bool ConsentObtained { get; set; }  
    public string? ConsentText { get; set; } // "Al firmar, acepta los términos bajo ESIGN Act..."  
    public DateTime ExpiryDate { get; set; } // Fecha límite para firmar  
  public  ICollection<EventSignature>? Firms { get; set; } 
  public ICollection<AnswerRequirement>? RequiredSignature { get; set; }


}