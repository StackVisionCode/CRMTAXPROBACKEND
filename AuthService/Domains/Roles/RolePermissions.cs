using AuthService.Domains.Permissions;
using Common;

namespace AuthService.Domains.Roles;

public class RolePermissions : BaseEntity
{
    public required Guid RoleId { get; set; }
    public required Guid PermissionsId { get; set; }
    public required virtual Role Role { get; set; }
    public required virtual Permission Permissions { get; set; }
}