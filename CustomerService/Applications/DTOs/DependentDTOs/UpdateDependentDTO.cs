using System.ComponentModel.DataAnnotations;

namespace CustomerService.DTOs.DependentDTOs;

public class UpdateDependentDTO
{
    [Key]
    public required Guid Id { get; set; }
    public required Guid CustomerId { get; set; }
    public string? FullName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public required Guid RelationshipId { get; set; }
}
