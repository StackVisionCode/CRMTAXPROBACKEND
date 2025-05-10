using Common;
using Domains.Firms;

namespace Domains.Signatures;

public class SignatureType : BaseEntity
{
   
    public required string Name { get; set; }
    public  string Description { get; set; } = string.Empty;
    public  ICollection<Firm>? Firms { get; set; } 

}
