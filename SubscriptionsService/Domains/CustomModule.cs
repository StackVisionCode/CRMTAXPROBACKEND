using Common;

namespace Domains;

public class CustomModule : BaseEntity
{
    public required Guid CustomPlanId { get; set; }
    public required Guid ModuleId { get; set; }
    public bool IsIncluded { get; set; } = true;

    // Navegación
    public virtual CustomPlan CustomPlan { get; set; } = null!;
    public virtual Module Module { get; set; } = null!;
}
