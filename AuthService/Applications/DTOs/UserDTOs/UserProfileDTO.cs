using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.UserDTOs;

public class UserProfileDTO
{
    [Key]
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public string? Address { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Domain { get; set; }
    public ICollection<string> RoleNames { get; set; } = new List<string>();
    public Guid CompanyId { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyBrand { get; set; }
}
