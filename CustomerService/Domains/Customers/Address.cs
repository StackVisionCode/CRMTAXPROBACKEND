using Common;

namespace CustomerService.Domains.Customers;

public class Address : BaseEntity
{
    public string? StreetAddress { get; set; }
    public string? ApartmentNumber { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string Country { get; set; } = "USA";
    public required Guid CustomerId { get; set; }
    public Guid CreatedByTaxUserId { get; set; }
    public Guid? LastModifiedByTaxUserId { get; set; }
    public virtual Customer? Customer { get; set; }
}
