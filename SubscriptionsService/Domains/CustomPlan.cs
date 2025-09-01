using Common;

namespace Domains;

public class CustomPlan : BaseEntity
{
    public Guid CompanyId { get; set; } // Referencia a la compañía (sin navegación, solo ID)
    public decimal Price { get; set; } // Precio personalizado acordado para este plan
    public int UserLimit { get; set; } // Límite de usuarios para este plan
    public bool IsActive { get; set; } = true;
    public DateTime? StartDate { get; set; } // Fecha de inicio del plan (null = inicio inmediato)
    public DateTime RenewDate { get; set; } // Fecha en que el plan debe renovarse (ej.: fin del período actual)
    public bool IsRenewed { get; set; } = false; // Indica si ya fue renovado manual/automáticamente
    public DateTime? RenewedDate { get; set; } // Fecha de la última renovación realizada (si existe)

    // Relación con los módulos personalizados incluidos/excluidos en este plan
    public virtual ICollection<CustomModule> CustomModules { get; set; } = new List<CustomModule>();
}
