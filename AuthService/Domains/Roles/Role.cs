using AuthService.Applications.Common;
using AuthService.Domains.Users;
using Common;

namespace AuthService.Domains.Roles;

public class Role : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public PortalAccess PortalAccess { get; set; } = PortalAccess.Staff;
    public ServiceLevel? ServiceLevel { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserCompanyRole> UserCompanyRoles { get; set; } =
        new List<UserCompanyRole>();
}
