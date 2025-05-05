using Common;
using Domain.Signatures;
namespace Domains.Requirements;

public class AnswerRequirement : BaseEntity
{
    public int CompanyId { get; set; }
    public int TaxUserId { get; set; }
    public int RequirementSignatureId { get; set; }
    public RequirementSignature? RequirementSignature { get; set; }
      public  ICollection<EventSignature>? Firms { get; set; } 
 

}
