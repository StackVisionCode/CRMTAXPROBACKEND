using System.ComponentModel.DataAnnotations;
using AuthService.DTOs.CustomModuleDTOs;

namespace AuthService.DTOs.CustomPlanDTOs;

public class NewCustomPlanDTO
{
    public required Guid CompanyId { get; set; }

    [Range(0, double.MaxValue)]
    public required decimal Price { get; set; }

    [Range(1, int.MaxValue)]
    public required int UserLimit { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? StartDate { get; set; }
    public DateTime RenewDate { get; set; } = DateTime.UtcNow.AddYears(1);

    // Módulos adicionales a incluir
    public ICollection<NewCustomModuleDTO>? CustomModules { get; set; } =
        new List<NewCustomModuleDTO>();
}
