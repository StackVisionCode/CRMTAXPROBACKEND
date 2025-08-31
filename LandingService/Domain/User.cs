using Common;

namespace LandingService.Domain;

public class User : BaseEntity
{
    public required string Name { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required bool IsActive { get; set; }
    public string? PhoneNumber { get; set; }
    public bool? Confirm { get; set; }
    public string? ConfirmToken { get; set; }
    public string? ResetPasswordToken { get; set; }
    public DateTime? ResetPasswordExpires { get; set; }
    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}
