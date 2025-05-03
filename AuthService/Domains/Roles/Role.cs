using AuthService.Domains.Users;
using Common;

namespace AuthService.Domains.Roles;

public class Role : BaseEntity
{
  public required string Name { get; set; }
  public string? Description { get; set; }
  public required virtual ICollection<TaxUser> TaxUser { get; set; }
}