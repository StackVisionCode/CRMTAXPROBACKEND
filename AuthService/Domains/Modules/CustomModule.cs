using AuthService.Domains.CustomPlans;
using AuthService.Domains.Modules;
using Common;

namespace AuthService.Domains.Modules;

/// <summary>
/// Relación uno a uno entre Module
/// Define qué módulos incluye cada servicio
/// </summary>
public class CustomModule : BaseEntity
{
    public required Guid CustomPlanId { get; set; }
    public required Guid ModuleId { get; set; }
    public bool IsIncluded { get; set; } = true;

    // Navegación
    public virtual CustomPlan CustomPlan { get; set; } = null!;
    public virtual Module Module { get; set; } = null!;
}
