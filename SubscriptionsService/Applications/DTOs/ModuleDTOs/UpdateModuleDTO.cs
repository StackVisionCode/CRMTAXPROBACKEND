using System.ComponentModel.DataAnnotations;

namespace DTOs.ModuleDTOs;

public class UpdateModuleDTO
{
    [Key]
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public string? Url { get; set; }
    public bool IsActive { get; set; } = true;
}
