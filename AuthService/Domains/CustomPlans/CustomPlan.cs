using AuthService.Domains.Companies;
using AuthService.Domains.Modules;
using Common;

namespace AuthService.Domains.CustomPlans;

/// <summary>
/// Planes personalizados que pueden tener las companies
/// Permite agregar módulos adicionales al servicio base
/// </summary>
public class CustomPlan : BaseEntity
{
    public required Guid CompanyId { get; set; }
    public required decimal Price { get; set; }
    public required int UserLimit { get; set; } // Límite de usuarios por plan personalizado
    public bool IsActive { get; set; } = true;
    public DateTime? StartDate { get; set; }
    public bool isRenewed { get; set; } = false;
    public DateTime? RenewedDate { get; set; }
    public DateTime RenewDate { get; set; } = DateTime.UtcNow;

    // Navegación
    public virtual Company Company { get; set; } = null!;
    public virtual ICollection<CustomModule> CustomModules { get; set; } = new List<CustomModule>();
}
