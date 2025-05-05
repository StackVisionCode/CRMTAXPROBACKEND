using Common;
using Domain.Signatures;
using Domains.Firms;

namespace Domains.Signatures;

public class SignatureEventType : BaseEntity
{
   
    public required string Name { get; set; }
    public  string Description { get; set; } = string.Empty;
    public  ICollection<EventSignature>? Firms { get; set; } 

}
