
using AuthService.Domains.Roles;
using AuthService.Domains.Sessions;
using Common;
using Users;

namespace AuthService.Domains.Users;

public class TaxUser : BaseEntity
{
  public required int TaxUserTypeId { get; set; }
  public int? CompanyId { get; set; }
  public required int RoleId { get; set; }
  public required string FullName { get; set; }
  public required string Email { get; set; }
  public required string Password { get; set; }
  public required bool IsActive { get; set; }
  public bool? Confirm { get; set; }
  public string? ConfirmToken { get; set; }
  public string? ResetPasswordToken { get; set; }
  public DateTime? ResetPasswordExpires { get; set; }
  public bool? Factor2 { get; set; }
  public string? Otp { get; set; }
  public DateTime? OtpExpires { get; set; }
  public required virtual ICollection<Session> Session { get; set; }
  public required virtual TaxUserType TaxUserType { get; set; }
  public required virtual Role Role { get; set; }
}
