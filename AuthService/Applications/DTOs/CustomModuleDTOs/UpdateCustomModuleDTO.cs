namespace AuthService.DTOs.CustomModuleDTOs;

public class UpdateCustomModuleDTO
{
    public required Guid Id { get; set; }
    public bool IsIncluded { get; set; } = true;
}
