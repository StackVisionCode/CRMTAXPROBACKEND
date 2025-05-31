using System.ComponentModel.DataAnnotations;

namespace CustomerService.DTOs.MaritalStatusDTOs;

public class ReadMaritalStatusDto
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
