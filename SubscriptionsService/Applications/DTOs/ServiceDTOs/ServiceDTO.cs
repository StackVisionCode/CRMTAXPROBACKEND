using System.ComponentModel.DataAnnotations;

namespace DTOs.ServiceDTOs;

public class ServiceDTO
{
    [Key]
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Title { get; set; }
    public required List<string> Features { get; set; }
    public required decimal Price { get; set; }
    public required int UserLimit { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<string> ModuleNames { get; set; } = new List<string>();
    public ICollection<Guid> ModuleIds { get; set; } = new List<Guid>();
    public DateTime CreatedAt { get; set; }
}
