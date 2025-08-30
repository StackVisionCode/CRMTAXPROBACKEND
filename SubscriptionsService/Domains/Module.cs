using Common;

namespace Domains;

/// <summary>
/// Módulos del sistema (Tax Returns, Invoicing, Reports, etc.)
/// </summary>
public class Module : BaseEntity
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public string? Url { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? ServiceId { get; set; }

    // Navegación
    public virtual Service? Service { get; set; }
    public virtual ICollection<CustomModule> CustomModules { get; set; } = new List<CustomModule>();
}
