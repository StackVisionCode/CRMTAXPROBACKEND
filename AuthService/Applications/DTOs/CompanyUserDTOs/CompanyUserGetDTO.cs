using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.CompanyUserDTOs;

public class CompanyUserGetDTO
{
    [Key]
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public ICollection<string> RoleNames { get; set; } = new List<string>();
    public required string Email { get; set; }
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public string? Position { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
