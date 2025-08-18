using System.ComponentModel.DataAnnotations;
using AuthService.Applications.Common;

namespace AuthService.DTOs.CompanyDTOs;

public class UpdateCompanyPlanDTO
{
    public required Guid CompanyId { get; set; }

    [EnumDataType(typeof(ServiceLevel))]
    public required ServiceLevel NewServiceLevel { get; set; }

    /// Precio personalizado opcional. Si no se especifica, usa el precio del servicio base.
    [Range(0, double.MaxValue, ErrorMessage = "Custom price must be 0 or greater")]
    public decimal? CustomPrice { get; set; }
    public int? CustomUserLimit { get; set; }

    /// Fecha de inicio del nuevo plan. Si no se especifica, usa la fecha actual.
    public DateTime? StartDate { get; set; }

    /// Fecha de fin del plan. Si no se especifica, el plan no expira.
    public DateTime? EndDate { get; set; }

    /// IDs de módulos adicionales a incluir (más allá de los del servicio base)
    public ICollection<Guid>? AdditionalModuleIds { get; set; }

    /// Indica si se debe forzar el cambio de plan incluso si hay usuarios que se desactivarán
    public bool ForceUserDeactivation { get; set; } = false;
}
