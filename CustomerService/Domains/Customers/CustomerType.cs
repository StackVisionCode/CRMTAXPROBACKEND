using Common;

namespace CustomerService.Domains.Customers;

public class CustomerType : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public virtual ICollection<Customer> Customers { get; set; } = new HashSet<Customer>();
}