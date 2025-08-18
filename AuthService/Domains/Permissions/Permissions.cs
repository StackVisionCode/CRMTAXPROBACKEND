using AuthService.Domains.Roles;
using Common;

namespace AuthService.Domains.Permissions;

public class Permission : BaseEntity
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string? Description { get; set; }
    public bool IsGranted { get; set; } = true;
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<CompanyPermission> CompanyPermissions { get; set; } =
        new List<CompanyPermission>();
}
