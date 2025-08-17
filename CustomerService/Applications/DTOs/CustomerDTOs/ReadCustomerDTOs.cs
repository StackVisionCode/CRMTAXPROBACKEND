using System.ComponentModel.DataAnnotations;

namespace CustomerService.DTOs.CustomerDTOs;

public class ReadCustomerDTO
{
    [Key]
    public required Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public string? CustomerType { get; set; }
    public string? CustomerTypeDescription { get; set; }
    public string? Occupation { get; set; }
    public string? MaritalStatus { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public required string SsnOrItin { get; set; }
    public required bool IsActive { get; set; }
    public required bool IsLogin { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByTaxUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? LastModifiedByTaxUserId { get; set; }
}
