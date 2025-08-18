using System.ComponentModel.DataAnnotations;

namespace CustomerService.DTOs.DependentDTOs;

public class ReadDependentDTO
{
    [Key]
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string? FullName { get; set; }
    public required DateTime DateOfBirth { get; set; }
    public Guid RelationshipId { get; set; }
    public string? Customer { get; set; }
    public string? Relationship { get; set; }

    // Información de auditoría
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByTaxUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? LastModifiedByTaxUserId { get; set; }
}
