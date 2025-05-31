using System.ComponentModel.DataAnnotations;

namespace CustomerService.DTOs.FilingStatusDTOs;

public class ReadFilingStatusDto
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
