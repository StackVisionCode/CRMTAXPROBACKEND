using Common;

namespace CustomerService.Domains.Customers;

public class Occupation : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }

    // Relación: una ocupación puede estar asociada a muchos clientes
    public virtual List<Customer>? Customers { get; set; }
}
