using Common;

namespace Domains;

public class Service : BaseEntity
{
    public string Name { get; set; } = string.Empty; // Nombre interno del plan (ej. "Basic", "Premium")
    public string Title { get; set; } = string.Empty; // Título descriptivo (ej. "Plan Básico")
    public string Description { get; set; } = string.Empty;
    public List<string> Features { get; set; } = new(); // Lista de características incluidas en el servicio
    public decimal Price { get; set; }
    public int UserLimit { get; set; }
    public bool IsActive { get; set; } = true;
    public ServiceLevel ServiceLevel { get; set; }

    // Relación con módulos base incluidos en este servicio
    public virtual ICollection<Module> Modules { get; set; } = new List<Module>();
}
