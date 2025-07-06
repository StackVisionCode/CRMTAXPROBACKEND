using System.ComponentModel.DataAnnotations;
using Common;

namespace AuthService.DTOs.RoleDTOs;

public class RoleDTO
{
    [Key]
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public PortalAccess PortalAccess { get; set; }
    public ICollection<string> PermissionCodes { get; set; } = new List<string>();
}
