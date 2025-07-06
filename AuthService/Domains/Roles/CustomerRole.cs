using Common;

namespace AuthService.Domains.Roles;

public class CustomerRole : BaseEntity
{
    public required Guid CustomerId { get; set; }
    public required Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
