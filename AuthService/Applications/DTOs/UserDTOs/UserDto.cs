using Common;

namespace UserDTOS;

public class UserDTO : BaseEntity
{
  public Guid TaxUserTypeId { get; set; }
  public Guid? CompanyId { get; set; }
  public required Guid RoleId { get; set; }
  public string? FullName { get; set; }
  public required string Email { get; set; }
  public required string Password { get; set; }
  public required bool IsActive { get; set; }
  public required bool Confirm { get; set; }
  public required string ConfirmToken { get; set; }
  public string? ResetPasswordToken { get; set; }
  public DateTime ResetPasswordExpires { get; set; }
  public bool Factor2 { get; set; }
  public string? Otp { get; set; }
  public DateTime OtpExpires { get; set; }
}