using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.CustomModuleDTOs;

public class CustomModuleDTO
{
    [Key]
    public Guid Id { get; set; }
    public required Guid CustomPlanId { get; set; }
    public required Guid ModuleId { get; set; }
    public bool IsIncluded { get; set; } = true;

    // Información del módulo
    public string? ModuleName { get; set; }
    public string? ModuleDescription { get; set; }
    public string? ModuleUrl { get; set; }
}
