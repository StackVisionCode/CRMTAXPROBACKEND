using System.ComponentModel.DataAnnotations;

namespace CustomerService.DTOs.TaxInformationDTOs;

public class ReadTaxInformationDTO
{
    [Key]
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid FilingStatusId { get; set; }
    public decimal LastYearAGI { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankRoutingNumber { get; set; }
    public required bool IsReturningCustomer { get; set; }
    public string? Customer { get; set; }
    public string? FilingStatus { get; set; }

    // Información de auditoría
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByTaxUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? LastModifiedByTaxUserId { get; set; }
}
