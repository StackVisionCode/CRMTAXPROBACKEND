using AuthService.Domains.Roles;
using Common;

namespace AuthService.Domains.Permissions;

public class Permission : BaseEntity
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string? Description { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
