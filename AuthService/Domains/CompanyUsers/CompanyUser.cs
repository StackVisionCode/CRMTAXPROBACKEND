using AuthService.Domains.Companies;
using AuthService.Domains.Sessions;
using Common;

namespace AuthService.Domains.CompanyUsers;

public class CompanyUser : BaseEntity
{
    public required Guid CompanyId { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required bool IsActive { get; set; }
    public bool? Confirm { get; set; }
    public string? ConfirmToken { get; set; }
    public string? ResetPasswordToken { get; set; }
    public DateTime? ResetPasswordExpires { get; set; }
    public bool? Factor2 { get; set; }
    public string? Otp { get; set; }
    public bool OtpVerified { get; set; }
    public DateTime? OtpExpires { get; set; }

    // Navegación
    public virtual required Company Company { get; set; }
    public virtual required CompanyUserProfile CompanyUserProfile { get; set; }
    public virtual ICollection<CompanyUserSession> CompanyUserSessions { get; set; } =
        new List<CompanyUserSession>();
    public virtual ICollection<CompanyUserRole> CompanyUserRoles { get; set; } =
        new List<CompanyUserRole>();
}
