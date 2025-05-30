namespace CustomerService.DTOs.DependentDTOs;

public class CreateDependentDTO
{
  public required Guid CustomerId { get; set; }
  public string? FullName { get; set; }
  public required DateTime DateOfBirth { get; set; }
  public required Guid RelationshipId { get; set; }
}