using AuthService.Domains.Roles;
using Common;

namespace AuthService.Domains.Users;

/// <summary>
///  Tabla de unión muchos‑a‑muchos entre TaxUser y Role.
/// </summary>
public class UserRole : BaseEntity
{
    public required Guid TaxUserId { get; set; }
    public required Guid RoleId { get; set; }
    public TaxUser TaxUser { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
