using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthService.DTOs.RoleDTOs;

public class RolePermissionDTO
{
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  [Key]
  public int Id { get; set; }
  public required int TaxUserId { get; set; }
  public required int RoleId { get; set; }
  public required int PermissionsId { get; set; }
}