using AuthService.Domains.Users;
using Common;

namespace AuthService.Domains.Companies;

public class Company : BaseEntity
{
  public string? FullName { get; set; }
  public string? CompanyName { get; set; } 
  public string? Address { get; set; }
  public string? Description { get; set; }
  public int UserLimit { get; set; } 
  public string? Brand { get; set; }
  public virtual ICollection<TaxUser>? TaxUsers { get; set; }
}