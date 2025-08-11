using AuthService.Domains.Addresses;
using AuthService.Domains.CustomPlans;
using AuthService.Domains.UserCompanies;
using AuthService.Domains.Users;
using Common;

namespace AuthService.Domains.Companies;

public class Company : BaseEntity
{
    public bool IsCompany { get; set; }
    public string? FullName { get; set; }
    public string? CompanyName { get; set; }
    public string? Brand { get; set; }
    public Guid? AddressId { get; set; }
    public virtual Address? Address { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public string? Domain { get; set; }
    public required Guid CustomPlanId { get; set; }
    public virtual CustomPlan CustomPlan { get; set; } = null!;
    public virtual ICollection<TaxUser> TaxUsers { get; set; } = new List<TaxUser>();
    public virtual ICollection<UserCompany> UserCompanies { get; set; } = new List<UserCompany>();
}
