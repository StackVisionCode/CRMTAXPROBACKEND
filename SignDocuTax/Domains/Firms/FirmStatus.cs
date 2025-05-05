using Common;


namespace Domains.Firms;

public class FirmStatus : BaseEntity
{
   
    public required string Name { get; set; }
    public  string Description { get; set; } = string.Empty;
    public  ICollection<Firm>? Firms { get; set; } 

}
