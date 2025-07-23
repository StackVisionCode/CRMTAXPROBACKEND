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
}
