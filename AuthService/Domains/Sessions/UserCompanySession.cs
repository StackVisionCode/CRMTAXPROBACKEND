using AuthService.Domains.UserCompanies;
using Common;

namespace AuthService.Domains.Sessions;

/// <summary>
/// Sesiones para usuarios de companies
/// </summary>
public class UserCompanySession : BaseEntity
{
    public required Guid UserCompanyId { get; set; }
    public required string TokenRequest { get; set; }
    public required DateTime ExpireTokenRequest { get; set; }
    public string? TokenRefresh { get; set; }
    public string? IpAddress { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public string? Device { get; set; }
    public bool IsRevoke { get; set; }

    // Navegación
    public virtual UserCompany UserCompany { get; set; } = null!;
}
