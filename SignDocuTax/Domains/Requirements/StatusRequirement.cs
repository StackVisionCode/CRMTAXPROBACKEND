using Common;


namespace Domains.Requirements;

public class StatusRequirement : BaseEntity
{
   
    public required string Name { get; set; }
    public  string Description { get; set; } = string.Empty;
    public  ICollection<RequirementSignature>? RequirementSignatures { get; set; } 

}
