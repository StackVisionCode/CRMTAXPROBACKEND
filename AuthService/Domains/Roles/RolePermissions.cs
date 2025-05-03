using AuthService.Domains.Permissions;
using AuthService.Domains.Users;
using Common;

namespace AuthService.Domains.Roles;

public class RolePermissions : BaseEntity
{
    public required int TaxUserId { get; set; }
    public required int RoleId { get; set; }
    public required int PermissionsId { get; set; }

    public required virtual TaxUser TaxUser { get; set; }
    public required virtual Role Role { get; set; }
    public required virtual Permission Permissions { get; set; }
}