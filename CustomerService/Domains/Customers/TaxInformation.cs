using Common;

namespace CustomerService.Domains.Customers;

public class TaxInformation : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Guid FilingStatusId { get; set; }
    public decimal LastYearAGI { get; set; } //Last Year Adjusted Gross Income
    public string? BankAccountNumber { get; set; }
    public string? BankRoutingNumber { get; set; }
    public required bool IsReturningCustomer { get; set; } // Indicates if the customer is returning for tax services
    public virtual Customer? Customer { get; set; }
    public virtual FilingStatus? FilingStatus { get; set; }
}
