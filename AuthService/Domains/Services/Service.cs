using AuthService.Domains.Companies;
using AuthService.Domains.Modules;
using Common;

namespace AuthService.Domains.Services;

/// <summary>
/// Servicios que puede contratar una company (Basic, Standard, Pro)
/// </summary>
public class Service : BaseEntity
{
    public required string Name { get; set; } // "Basic", "Standard", "Pro"
    public required string Description { get; set; }
    public required decimal Price { get; set; }
    public required int UserLimit { get; set; } // Límite de usuarios por servicio
    public bool IsActive { get; set; } = true;

    // Navegación
    public virtual ICollection<Module> Modules { get; set; } = new List<Module>();
    public virtual ICollection<Company> Companies { get; set; } = new List<Company>();
}
