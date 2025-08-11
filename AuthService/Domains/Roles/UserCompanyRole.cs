using AuthService.Domains.Permissions;
using AuthService.Domains.UserCompanies;
using Common;

namespace AuthService.Domains.Roles;

/// <summary>
/// Relación muchos a muchos entre UserCompany y Role
/// </summary>
public class UserCompanyRole : BaseEntity
{
    public required Guid UserCompanyId { get; set; }
    public required Guid RoleId { get; set; }

    // Navegación
    public virtual UserCompany UserCompany { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
    public virtual ICollection<CompanyPermission> CompanyPermissions { get; set; } =
        new List<CompanyPermission>();
}
