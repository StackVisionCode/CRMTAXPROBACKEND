using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.UserDTOs;

public class UserGetDTO
{
    [Key]
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public ICollection<string> RoleNames { get; set; } = new List<string>();
    public string? FullName { get; set; }
    public required string Email { get; set; }
}
