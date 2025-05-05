using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthService.DTOs.UserDTOs;

public class UserGetDTO
{
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  [Key]
  public int Id { get; set; }
  public required int TaxUserTypeId { get; set; }
  public int? CompanyId { get; set; }
  public required int RoleId { get; set; }
  public string? FullName { get; set; }
  public required string Email { get; set; }
}


