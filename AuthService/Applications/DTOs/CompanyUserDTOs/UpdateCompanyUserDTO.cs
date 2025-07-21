using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.CompanyUserDTOs;

public class UpdateCompanyUserDTO
{
    [Key]
    public required Guid Id { get; set; }

    public string? Name { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? Password { get; set; }
    public string? Address { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Position { get; set; }
    public bool? IsActive { get; set; }
}
