using AuthService.Domains.Roles;
using Common;

namespace AuthService.Domains.Permissions;

public class Permission : BaseEntity
{
  public required string Name { get; set; }
  public string? Description { get; set; }
  public required virtual ICollection<RolePermissions> RolePermissions { get; set; }
}