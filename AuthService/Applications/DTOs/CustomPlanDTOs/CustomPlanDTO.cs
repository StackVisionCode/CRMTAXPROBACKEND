using System.ComponentModel.DataAnnotations;
using AuthService.DTOs.CustomModuleDTOs;

namespace AuthService.DTOs.CustomPlanDTOs;

public class CustomPlanDTO
{
    [Key]
    public Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required decimal Price { get; set; }
    public required int UserLimit { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? StartDate { get; set; }
    public bool isRenewed { get; set; } = false;
    public DateTime? RenewedDate { get; set; }
    public DateTime RenewDate { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyDomain { get; set; }
    public ICollection<CustomModuleDTO> CustomModules { get; set; } = new List<CustomModuleDTO>();
    public ICollection<string> AdditionalModuleNames { get; set; } = new List<string>();
}
