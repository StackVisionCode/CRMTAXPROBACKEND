using System.ComponentModel.DataAnnotations;

namespace CustomerService.DTOs.OccupationDTOs;

public class ReadOccupationDTO
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
