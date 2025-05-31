using Common;

namespace CustomerService.Domains.Customers;

public class PreferredContact : BaseEntity
{
    public required string Name { get; set; } = default!;
    public virtual List<ContactInfo>? Contacts { get; set; }
}
