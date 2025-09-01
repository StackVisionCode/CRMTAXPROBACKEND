namespace DTOs.CustomModuleDTOs;

public class NewCustomModuleDTO
{
    public required Guid ModuleId { get; set; }
    public bool IsIncluded { get; set; } = true;
}
