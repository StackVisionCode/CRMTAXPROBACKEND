using Common;

namespace AuthService.Domains.Sessions;

public class CustomerSession : BaseEntity
{
    public Guid CustomerId { get; set; }
    public required string TokenRequest { get; set; }
    public required DateTime ExpireTokenRequest { get; set; }
    public string? TokenRefresh { get; set; }
    public string? IpAddress { get; set; }
    public string? Device { get; set; }
    public bool IsRevoke { get; set; }
}
