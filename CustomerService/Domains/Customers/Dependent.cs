using Common;

namespace CustomerService.Domains.Customers;

public class Dependent : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string? FullName { get; set; }
    public required DateTime DateOfBirth { get; set; }
    public Guid RelationshipId { get; set; }
    public virtual Customer? Customer { get; set; }
    public virtual Relationship? Relationship { get; set; }
}
