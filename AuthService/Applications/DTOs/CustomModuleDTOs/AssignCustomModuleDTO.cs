namespace AuthService.DTOs.CustomModuleDTOs;

public class AssignCustomModuleDTO
{
    public required Guid CustomPlanId { get; set; }
    public required Guid ModuleId { get; set; }
    public bool IsIncluded { get; set; } = true;
}
