using System.ComponentModel.DataAnnotations;

namespace CustomerService.DTOs.PreferredContactDTOs;

public class ReadPreferredContactDTO
{
    [Key]
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
