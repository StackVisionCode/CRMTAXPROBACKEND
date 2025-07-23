using AuthService.Domains.CompanyUsers;
using Common;

namespace AuthService.Domains.Sessions;

public class CompanyUserSession : BaseEntity
{
    public required Guid CompanyUserId { get; set; }
    public required string TokenRequest { get; set; }
    public required DateTime ExpireTokenRequest { get; set; }
    public string? TokenRefresh { get; set; }
    public string? IpAddress { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public string? Device { get; set; }
    public bool IsRevoke { get; set; }

    // Navegación
    public virtual CompanyUser? CompanyUser { get; set; }
}
