using AuthService.Domains.Users;
using Common;

namespace AuthService.Domains.Roles;

public class Role : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
