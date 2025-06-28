using AuthService.Domains.Permissions;
using Common;

namespace AuthService.Domains.Roles;

public class RolePermission : BaseEntity
{
    public required Guid RoleId { get; set; }
    public required Guid PermissionId { get; set; }
    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
