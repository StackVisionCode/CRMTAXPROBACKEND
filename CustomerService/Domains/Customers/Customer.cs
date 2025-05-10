using Common;

namespace CustomerService.Domains.Customers;

public class Customer : BaseEntity
{
    public int CompanyId { get; set; }
    public int TaxUserId { get; set; }
    public int ContactId { get; set; }
    public int TeamMemberId { get; set; }
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public required string SSN { get; set; }
    public required string Email  { get; set; }
    public required int CustomerTypeId { get; set; }
    public virtual CustomerType? CustomerType { get; set; }
}