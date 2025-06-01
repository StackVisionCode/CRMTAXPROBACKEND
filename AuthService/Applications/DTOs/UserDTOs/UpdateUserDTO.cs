using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.UserDTOs;

public class UpdateUserDTO
{
    [Key]
    public required Guid Id { get; set; }

    [Required]
    public string Name { get; set; } = default!;

    [Required]
    public string LastName { get; set; } = default!;

    [Required]
    public string Phone { get; set; } = default!;

    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    public string? Address { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Domain { get; set; }
    public bool? IsActive { get; set; }
}
