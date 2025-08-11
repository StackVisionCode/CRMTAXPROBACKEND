using AuthService.Domains.Addresses;
using AuthService.Domains.Companies;
using AuthService.Domains.Permissions;
using AuthService.Domains.Roles;
using AuthService.Domains.Sessions;
using Common;

namespace AuthService.Domains.UserCompanies;

public class UserCompany : BaseEntity
{
    public required Guid CompanyId { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required bool IsActive { get; set; }
    public DateTime? IsActiveDate { get; set; }
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PhotoUrl { get; set; }
    public bool? Confirm { get; set; }
    public string? ConfirmToken { get; set; }
    public string? ResetPasswordToken { get; set; }
    public DateTime? ResetPasswordExpires { get; set; }
    public bool? Factor2 { get; set; }
    public string? Otp { get; set; }
    public bool OtpVerified { get; set; }
    public DateTime? OtpExpires { get; set; }

    // Dirección (mismo patrón que TaxUser)
    public virtual Guid? AddressId { get; set; }
    public virtual Address? Address { get; set; }

    // Navegación
    public virtual Company Company { get; set; } = null!;
    public virtual ICollection<UserCompanyRole> UserCompanyRoles { get; set; } =
        new List<UserCompanyRole>();
    public virtual ICollection<UserCompanySession> UserCompanySessions { get; set; } =
        new List<UserCompanySession>();
    public virtual ICollection<CompanyPermission> CompanyPermissions { get; set; } =
        new List<CompanyPermission>();
}
