using System.ComponentModel.DataAnnotations;

namespace CustomerService.DTOs.TaxInformationDTOs;

public class ReadTaxInformationDTO
{
  [Key]
  public required Guid Id { get; set; }
  public string? Customer { get; set; }
  public string? FilingStatus { get; set; }
  public decimal LastYearAGI { get; set; } //Last Year Adjusted Gross Income
  public string? BankAccountNumber { get; set; }
  public string? BankRoutingNumber { get; set; }
  public required bool IsReturningCustomer { get; set; } // Indicates if the customer is returning for tax services
}