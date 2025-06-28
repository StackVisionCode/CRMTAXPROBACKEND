using AuthService.Domains.Companies;
using AuthService.Domains.Roles;
using AuthService.Domains.Sessions;
using Common;

namespace AuthService.Domains.Users;

public class TaxUser : BaseEntity
{
    public Guid? CompanyId { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public string? Domain { get; set; }
    public required bool IsActive { get; set; }
    public bool? Confirm { get; set; }
    public string? ConfirmToken { get; set; }
    public string? ResetPasswordToken { get; set; }
    public DateTime? ResetPasswordExpires { get; set; }
    public bool? Factor2 { get; set; }
    public string? Otp { get; set; }
    public bool OtpVerified { get; set; }
    public DateTime? OtpExpires { get; set; }
    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual required TaxUserProfile TaxUserProfile { get; set; }
    public virtual Company? Company { get; set; }
}
