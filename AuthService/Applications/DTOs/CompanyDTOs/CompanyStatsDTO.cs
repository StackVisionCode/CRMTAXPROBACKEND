using AuthService.DTOs.UserDTOs;

namespace AuthService.DTOs.CompanyDTOs;

/// <summary>
/// ACTUALIZADO: DTO para estadísticas completas de una company
/// </summary>
public class CompanyStatsDTO
{
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? Domain { get; set; }
    public bool IsCompany { get; set; }

    // CAMBIO: Solo estadísticas de TaxUsers ahora
    public int TotalUsers { get; set; } // Total TaxUsers
    public int ActiveUsers { get; set; } // TaxUsers activos
    public int OwnerCount { get; set; } = 1; // Siempre 1 Owner
    public int RegularUsers { get; set; } // TaxUsers que no son Owner

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
///  DTO para obtener todos los usuarios de la company
/// </summary>
public class CompanyUsersCompleteDTO
{
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public bool IsCompany { get; set; }

    // Solo TaxUsers ahora (incluyendo Owner y Users regulares)
    public List<UserGetDTO> Users { get; set; } = new();
    public UserGetDTO? Owner => Users.FirstOrDefault(u => u.IsOwner);
    public List<UserGetDTO> RegularUsers => Users.Where(u => !u.IsOwner).ToList();

    public int TotalUsers => Users.Count;
    public int OwnerCount => Owner != null ? 1 : 0;
    public int RegularUsersCount => RegularUsers.Count;

    // Plan limits
    public int ServiceUserLimit { get; set; }
    public bool IsWithinLimits => RegularUsersCount <= ServiceUserLimit;

    // Totales
    public int ActiveUsers => Users.Count(u => u.IsActive);
}
