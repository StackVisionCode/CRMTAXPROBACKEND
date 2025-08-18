namespace AuthService.DTOs.ModuleDTOs;

public class AssignModuleToServiceDTO
{
    public required Guid ModuleId { get; set; }

    public required Guid ServiceId { get; set; }
}
