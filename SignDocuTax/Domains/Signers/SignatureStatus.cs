using Common;

namespace Domains.Signers;
public class SignatureStatus : BaseEntity
{
    public string Name { get; set; } // "Pending", "Completed", "Rejected", "Expired"
    public string Description { get; set; }
}