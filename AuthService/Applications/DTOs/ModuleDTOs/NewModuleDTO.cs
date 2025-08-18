namespace AuthService.DTOs.ModuleDTOs;

public class NewModuleDTO
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public string? Url { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? ServiceId { get; set; }
}
