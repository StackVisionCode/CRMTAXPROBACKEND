using AuthService.Domains.Addresses;
using AuthService.Domains.Companies;
using AuthService.Domains.Permissions;
using AuthService.Domains.Sessions;
using Common;

namespace AuthService.Domains.Users;

public class TaxUser : BaseEntity
{
    public required Guid CompanyId { get; set; }
    public virtual Company? Company { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required bool IsActive { get; set; }
    public bool IsOwner { get; set; } = false;
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

    // Dirección
    public virtual Guid? AddressId { get; set; }
    public virtual Address? Address { get; set; }
    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<CompanyPermission> CompanyPermissions { get; set; } =
        new List<CompanyPermission>();
}
