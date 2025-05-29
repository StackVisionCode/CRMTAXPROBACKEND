using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.UserDTOs;

public class UserGetDTO
{
  [Key]
  public Guid Id { get; set; }
  public Guid? CompanyId { get; set; }
  public required Guid RoleId { get; set; }
  public string? FullName { get; set; }
  public required string Email { get; set; }
}


