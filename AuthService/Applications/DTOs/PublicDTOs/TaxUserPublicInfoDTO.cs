using AuthService.Applications.Common;

namespace DTOs.PublicDTOs;

public class TaxUserPublicInfoDTO
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsOwner { get; set; }

    // Información limitada de la compañía
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyBrand { get; set; }
    public string? CompanyDomain { get; set; }
    public ServiceLevel CompanyServiceLevel { get; set; }

    // Solo roles básicos (filtrados para seguridad)
    public List<string> BasicRoles { get; set; } = new();

    // Información de contacto básica de la company
    public string? CompanyPhone { get; set; }
    public string? CompanyCity { get; set; }
    public string? CompanyState { get; set; }
}
