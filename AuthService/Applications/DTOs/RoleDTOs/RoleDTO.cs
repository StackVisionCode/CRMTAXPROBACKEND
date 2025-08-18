using System.ComponentModel.DataAnnotations;
using AuthService.Applications.Common;
using Common;

namespace AuthService.DTOs.RoleDTOs;

public class RoleDTO
{
    [Key]
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public PortalAccess PortalAccess { get; set; }
    public ServiceLevel? ServiceLevel { get; set; }
    public ICollection<string> PermissionCodes { get; set; } = new List<string>();
    public DateTime CreatedAt { get; set; }
}
