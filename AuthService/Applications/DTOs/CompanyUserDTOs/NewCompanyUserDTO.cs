using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.CompanyUserDTOs;

public class NewCompanyUserDTO
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; } = default!;

    [Required]
    public string LastName { get; set; } = default!;

    [Required]
    public string Phone { get; set; } = default!;

    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    [Required, MinLength(8)]
    public string Password { get; set; } = default!;

    public string? Address { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Position { get; set; }

    [Required]
    public Guid CompanyId { get; set; }
}
