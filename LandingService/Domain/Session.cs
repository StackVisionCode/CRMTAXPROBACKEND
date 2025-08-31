using Common;

namespace LandingService.Domain;

public class Session : BaseEntity
{
    public Guid UserId { get; set; }
    public string? Token { get; set; }
    public required string TokenRequest { get; set; }
    public required DateTime ExpireTokenRequest { get; set; }
    public string? TokenRefresh { get; set; }
    public string? IpAddress { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public string? Location { get; set; }
    public string? Device { get; set; }
    public bool IsRevoke { get; set; }
    public virtual User? User { get; set; }
}
