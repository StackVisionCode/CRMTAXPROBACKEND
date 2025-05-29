using AuthService.Domains.Companies;
using Common;

namespace AuthService.Domains.Users;

public class TaxUserProfile : BaseEntity
{
  public required Guid TaxUserId { get; set; }
  public string? Name { get; set; }
  public string? LastName { get; set; }
  public string? PhoneNumber { get; set; }
  public string? Address { get; set; }
  public string? PhotoUrl { get; set; }

  // Relacion Inversa para Usuarios
  public virtual TaxUser? TaxUser { get; set; } 
}

