namespace CustomerService.DTOs.TaxInformationDTOs;

public class CreateTaxInformationDTOs
{
    public required Guid CustomerId { get; set; }
    public required Guid FilingStatusId { get; set; }
    public decimal LastYearAGI { get; set; } //Last Year Adjusted Gross Income
    public string? BankAccountNumber { get; set; }
    public string? BankRoutingNumber { get; set; }
    public required bool IsReturningCustomer { get; set; } // Indicates if the customer is returning for tax services
}
