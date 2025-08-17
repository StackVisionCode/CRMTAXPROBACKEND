using Common;

namespace AuthService.DTOs.SessionDTOs;

/// <summary>
/// Resultado de búsqueda de TaxUser para login
/// </summary>
public class TaxUserLoginResult
{
    public Guid UserId { get; set; }
    public required string Email { get; set; }
    public required string HashedPassword { get; set; }
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; }
    public bool IsConfirmed { get; set; }
    public bool IsOwner { get; set; } // NUEVO: Indica si es el Owner/Administrator
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyFullName { get; set; }
    public string? CompanyDomain { get; set; }
    public bool IsCompany { get; set; }
    public bool CompanyIsActive { get; set; } // NUEVO: Estado del CustomPlan
}

/// <summary>
/// Resultado de rol para login
/// </summary>
public class RoleResult
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public PortalAccess PortalAccess { get; set; }
}

/// <summary>
/// Resultado de permiso para login
/// </summary>
public class PermissionResult
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
}
