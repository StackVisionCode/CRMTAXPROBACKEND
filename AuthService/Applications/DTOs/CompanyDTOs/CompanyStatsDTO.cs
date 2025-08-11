using AuthService.DTOs.UserCompanyDTOs;
using AuthService.DTOs.UserDTOs;

namespace AuthService.DTOs.CompanyDTOs;

/// <summary>
/// DTO para estadísticas completas de una company
/// </summary>
public class CompanyStatsDTO
{
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? Domain { get; set; }
    public bool IsCompany { get; set; }

    // Estadísticas de preparadores (TaxUsers)
    public int TotalPreparers { get; set; }
    public int ActivePreparers { get; set; }

    // Estadísticas de empleados (UserCompanies)
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }

    // Totales combinados
    public int TotalUsers => TotalPreparers + TotalEmployees;
    public int ActiveUsers => ActivePreparers + ActiveEmployees;

    // Plan información
    public decimal CustomPlanPrice { get; set; }
    public bool CustomPlanIsActive { get; set; }
    public int ServiceUserLimit { get; set; }
    public bool IsWithinLimits { get; set; }

    // Actividad
    public int ActiveSessions { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// ✅ OPCIONAL: DTO combinado para obtener toda la información de usuarios
/// Útil cuando necesitas tanto preparadores como empleados en una sola llamada
/// </summary>
public class CompanyUsersCompleteDTO
{
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public bool IsCompany { get; set; }

    // Preparadores (TaxUsers)
    public List<UserGetDTO> Preparers { get; set; } = new();
    public int TotalPreparers => Preparers.Count;

    // Empleados (UserCompanies)
    public List<UserCompanyDTO> Employees { get; set; } = new();
    public int TotalEmployees => Employees.Count;

    // Plan limits
    public int ServiceUserLimit { get; set; }
    public bool IsWithinLimits => TotalEmployees <= ServiceUserLimit;

    // Totales
    public int TotalUsers => TotalPreparers + TotalEmployees;
}
