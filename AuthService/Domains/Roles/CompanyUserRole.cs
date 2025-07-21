using AuthService.Domains.Roles;
using Common;

namespace AuthService.Domains.CompanyUsers;

public class CompanyUserRole : BaseEntity
{
    public required Guid CompanyUserId { get; set; }
    public required Guid RoleId { get; set; }

    // Navegación
    public CompanyUser CompanyUser { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
