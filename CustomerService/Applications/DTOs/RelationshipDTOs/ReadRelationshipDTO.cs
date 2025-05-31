using System.ComponentModel.DataAnnotations;

namespace CustomerService.DTOs.RelationshipDTOs;

public class ReadRelationshipDTO
{
    [Key]
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
