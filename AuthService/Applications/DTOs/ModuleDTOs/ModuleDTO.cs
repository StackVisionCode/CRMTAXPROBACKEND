using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.ModuleDTOs;

public class ModuleDTO
{
    [Key]
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public string? Url { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? ServiceId { get; set; }
    public string? ServiceName { get; set; }
}
