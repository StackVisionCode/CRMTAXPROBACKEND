
using AuthService.Domains.Users;
using Common;

namespace AuthService.Domains.Sessions;

public class Session : BaseEntity
{
    public required string SessionUid { get; set; }
    public int TaxUserId { get; set; }
    public required string TokenRequest { get; set; }
    public required DateTime ExpireTokenRequest { get; set; }
    public string? TokenRefresh { get; set; }  
    public string? IpAddress { get; set; }
    public string? Latitude { get; set; }
    public string?  Logintude { get; set; }
    public string? Device { get; set; }
    public bool IsRevoke { get; set; }
    public required virtual TaxUser TaxUser { get; set; }
}
