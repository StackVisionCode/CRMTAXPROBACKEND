
using Common;

namespace CustomerService.Domains.Customers;

public class Address :BaseEntity
{
      public string? StreetAddress { get; set; }
    public string? ApartmentNumber { get; set; }
    public string City { get; set; } = default!;
    public string State { get; set; } = default!;
    public string ZipCode { get; set; } = default!;
    public string Country { get; set; } = "USA";

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
}