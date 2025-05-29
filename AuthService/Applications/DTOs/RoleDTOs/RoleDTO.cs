using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.RoleDTOs;

public class RoleDTO
{
  [Key]
  public Guid Id { get; set; }
  public required string Name { get; set; }
  public string? Description { get; set; }
}