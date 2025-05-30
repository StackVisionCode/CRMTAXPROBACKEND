using System.ComponentModel.DataAnnotations;

namespace CustomerService.DTOs.CustomerDTOs;

public class ReadCustomerDTO
{
    [Key]
    public required Guid Id { get; set; }
    public string? Occupation { get; set; }
    public string? MaritalStatus { get; set; }
    public  string? FirstName { get; set; }
    public  string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public required string SsnOrItin { get; set; }  
    public required bool IsActive { get; set; }
    public required bool IsLogin { get; set; }
}