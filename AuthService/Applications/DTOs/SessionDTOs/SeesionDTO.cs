using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthService.DTOs.SessionDTOs;

public class SessionDTO
{
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  [Key]
  public int Id { get; set; }
  public required int TaxUserId { get; set; }
  public required string TokenRequest { get; set; } 
  public DateTime ExpireTokenRequest { get; set; }
  public string? TokenRefresh { get; set; } 
  public string? IpAddress { get; set; }
  public string? Location { get; set; } 
  public string? Device { get; set; }
    public required bool IsRevoke { get; set; }=false;
}