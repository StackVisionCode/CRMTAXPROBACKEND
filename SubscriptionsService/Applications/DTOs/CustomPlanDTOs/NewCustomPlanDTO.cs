using System.ComponentModel.DataAnnotations;
using Common;
using DTOs.CustomModuleDTOs;

namespace DTOs.CustomPlanDTOs;

public class NewCustomPlanDTO
{
    public required Guid CompanyId { get; set; }

    [Range(1, 3)]
    public required ServiceLevel ServiceLevel { get; set; } // Usar enum

    public bool IsActive { get; set; } = true;
    public DateTime? StartDate { get; set; }
    public DateTime RenewDate { get; set; } = DateTime.UtcNow.AddYears(1);

    // Módulos adicionales personalizados (opcional)
    public ICollection<NewCustomModuleDTO>? CustomModules { get; set; } =
        new List<NewCustomModuleDTO>();
}
