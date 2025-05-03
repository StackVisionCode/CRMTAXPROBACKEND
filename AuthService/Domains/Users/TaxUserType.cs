using AuthService.Domains.Users;
using Common;

namespace Users;

public class TaxUserType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public virtual ICollection<TaxUser> TaxUser { get; set; } = new List<TaxUser>();
}