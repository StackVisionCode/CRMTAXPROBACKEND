using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.RoleDTOs;

public class RolePermissionDTO
{
    [Key]
    public Guid Id { get; set; }
    public required Guid RoleId { get; set; }
    public required Guid PermissionsId { get; set; }
}
