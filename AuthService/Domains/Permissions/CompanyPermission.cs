using AuthService.Domains.Roles;
using AuthService.Domains.Users;
using Common;

namespace AuthService.Domains.Permissions;

/// <summary>
/// Permisos personalizados que un Administrator (Owner) puede asignar/quitar a sus Users
/// Esto permite granular control sobre qué puede hacer cada User dentro de la company
/// </summary>
public class CompanyPermission : BaseEntity
{
    public required Guid TaxUserId { get; set; } // User al que se le asigna/quita el permiso
    public required Guid PermissionId { get; set; } // Permiso específico
    public bool IsGranted { get; set; } = true; // true = granted, false = revoked
    public string? Description { get; set; }

    // Navegación
    public virtual TaxUser TaxUser { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}
