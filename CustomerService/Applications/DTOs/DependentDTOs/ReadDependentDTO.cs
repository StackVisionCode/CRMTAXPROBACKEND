using System.ComponentModel.DataAnnotations;

namespace CustomerService.DTOs.DependentDTOs;

public class ReadDependentDTO
{
  [Key]
  public required Guid Id { get; set; }
  public string? FullName { get; set; }
  public DateTime DateOfBirth { get; set; }
  public string? Customer { get; set; }
  public string? Relationship { get; set; }
}