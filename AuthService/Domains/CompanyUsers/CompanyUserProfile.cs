using Common;

namespace AuthService.Domains.CompanyUsers;

public class CompanyUserProfile : BaseEntity
{
    public required Guid CompanyUserId { get; set; }
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Position { get; set; } // Cargo en la empresa

    // Relación Inversa
    public virtual CompanyUser? CompanyUser { get; set; }
}
