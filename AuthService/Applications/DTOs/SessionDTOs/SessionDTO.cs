using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.SessionDTOs;

public class SessionDTO
{
    [Key]
    public Guid Id { get; set; }
    public required Guid TaxUserId { get; set; }
    public required string TokenRequest { get; set; }
    public DateTime ExpireTokenRequest { get; set; }
    public string? TokenRefresh { get; set; }
    public string? IpAddress { get; set; }
    public string? Location { get; set; }
    public string? Device { get; set; }
    public bool IsRevoke { get; set; } = false;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO extendido con información del usuario para las vistas de administración
/// </summary>
public class SessionWithUserDTO : SessionDTO
{
    // Información del usuario
    public string UserEmail { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? UserLastName { get; set; }
    public string? UserPhotoUrl { get; set; }
    public bool UserIsActive { get; set; }
    public bool UserIsOwner { get; set; }

    // Información de geolocalización
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Estado de la sesión
    public bool IsActive => !IsRevoke && ExpireTokenRequest > DateTime.UtcNow;
    public string Status => IsActive ? "Active" : (IsRevoke ? "Revoked" : "Expired");

    // Duración de la sesión
    public TimeSpan SessionDuration => ExpireTokenRequest - CreatedAt;
    public string DurationText => FormatDuration(SessionDuration);

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
            return $"{(int)duration.TotalDays}d {duration.Hours}h";
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        return $"{duration.Minutes}m";
    }

    // Información de seguridad
    public bool IsSuspicious { get; set; }
    public List<string> SecurityFlags { get; set; } = new();
}

/// <summary>
/// DTO para estadísticas de sesiones de la empresa
/// </summary>
public class CompanySessionStatsDTO
{
    public Guid CompanyId { get; set; }
    public int TotalActiveSessions { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int UniqueLocations { get; set; }
    public int UniqueDevices { get; set; }

    // Estadísticas por período
    public int SessionsLast24Hours { get; set; }
    public int SessionsLast7Days { get; set; }
    public int SessionsLast30Days { get; set; }

    // Top ubicaciones y dispositivos
    public List<LocationStats> TopLocations { get; set; } = new();
    public List<DeviceStats> TopDevices { get; set; } = new();

    // Actividad por hora (para gráficos)
    public Dictionary<int, int> ActivityByHour { get; set; } = new(); // Hora del día -> Count
    public Dictionary<string, int> ActivityByDay { get; set; } = new(); // Fecha -> Count
}

public class LocationStats
{
    public string Location { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? City { get; set; }
    public int SessionCount { get; set; }
    public int UserCount { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class DeviceStats
{
    public string Device { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public int UserCount { get; set; }
    public DateTime LastUsed { get; set; }
}

/// <summary>
/// Request para revocar una sesión específica
/// </summary>
public class RevokeSessionRequest
{
    public required Guid SessionId { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Request para revocar múltiples sesiones
/// </summary>
public class RevokeBulkSessionsRequest
{
    public required List<Guid> SessionIds { get; set; } = new();
    public string? Reason { get; set; }
}

/// <summary>
/// Request para revocar todas las sesiones de un usuario
/// </summary>
public class RevokeUserSessionsRequest
{
    public required Guid TargetUserId { get; set; }
    public string? Reason { get; set; }
}
