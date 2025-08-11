using AuthService.Domains.Roles;
using AuthService.Domains.UserCompanies;
using Common;

namespace AuthService.Domains.Permissions;

/// <summary>
/// Permisos personalizados que una company puede asignar/quitar a sus usuarios
/// Esto permite granular control sobre qué puede hacer cada UserCompany
/// </summary>
public class CompanyPermission : BaseEntity
{
    public required Guid UserCompanyId { get; set; }
    public required Guid UserCompanyRoleId { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public bool IsGranted { get; set; } = true; // true = granted, false = revoked
    public string? Description { get; set; }

    // Navegación
    public virtual UserCompany UserCompany { get; set; } = null!;
    public virtual UserCompanyRole UserCompanyRole { get; set; } = null!;
}
