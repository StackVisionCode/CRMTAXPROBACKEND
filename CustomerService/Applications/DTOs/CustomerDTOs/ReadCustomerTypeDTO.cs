using System.ComponentModel.DataAnnotations;

namespace CustomerService.DTOs.CustomerDTOs;

public class ReadCustomerTypeDTO
{
    [Key]
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
