using System.ComponentModel.DataAnnotations;
using Common;

namespace LandingService.Domain;

public class User:BaseEntity
{


    public required string Name { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? CompanyName { get; set; }
    public required string Password { get; set; }
    public required bool IsActive { get; set; }
    public string? PhoneNumber { get; set; }
    public bool? Confirm { get; set; }
    public string? ConfirmToken { get; set; }
    public string? ResetPasswordToken { get; set; }
    public DateTime? ResetPasswordExpires { get; set; }
    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
    public User()
    {
        Id = Guid.NewGuid();
        IsActive = false;
        Confirm = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt=null;
        DeleteAt=null;
        

    }
}
