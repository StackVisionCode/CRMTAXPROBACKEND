using AuthService.Domains.Modules;
using Common;

namespace AuthService.Domains.Services;

/// <summary>
/// Servicios que puede contratar una company (Basic, Standard, Pro)
/// </summary>
public class Service : BaseEntity
{
    public required string Name { get; set; } // "Basic", "Standard", "Pro"
    public required string Title { get; set; } // "Professional Plan", "Enterprise Solution"
    public required string Description { get; set; }
    public required List<string> Features { get; set; } // ["Feature 1", "Feature 2"]
    public required decimal Price { get; set; }
    public required int UserLimit { get; set; } // Límite de usuarios por servicio
    public bool IsActive { get; set; } = true;

    // Navegación
    public virtual ICollection<Module> Modules { get; set; } = new List<Module>();
}
