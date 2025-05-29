namespace AuthService.DTOs.UserDTOs;

public class UserProfileDTO
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public string? Address { get; set; }
    public string? PhotoUrl { get; set; }
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? CompanyBrand { get; set; }
}