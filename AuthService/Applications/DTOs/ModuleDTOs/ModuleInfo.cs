namespace AuthService.DTOs.ModuleDTOs;

/// <summary>
/// Clase helper para información de módulos
/// </summary>
public class ModuleInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ServiceId { get; set; }
}
