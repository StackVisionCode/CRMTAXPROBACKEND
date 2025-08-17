using System.ComponentModel.DataAnnotations;

namespace CustomerService.DTOs.TaxInformationDTOs;

public class UpdateTaxInformationDTOs
{
    [Key]
    public required Guid Id { get; set; } // Unique identifier for the tax information record
    public required Guid CustomerId { get; set; }
    public required Guid FilingStatusId { get; set; }
    public decimal LastYearAGI { get; set; } //Last Year Adjusted Gross Income
    public string? BankAccountNumber { get; set; }
    public string? BankRoutingNumber { get; set; }
    public required bool IsReturningCustomer { get; set; } // Indicates if the customer is returning for tax services
    public Guid? LastModifiedByTaxUserId { get; set; }
}
