using System.ComponentModel.DataAnnotations;
using Applications.DTOs.CompanyDTOs;

namespace AuthService.DTOs.UserDTOs;

public class UpdateUserDTO
{
    [Key]
    public required Guid Id { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [MinLength(8)]
    public string? Password { get; set; }

    // Información personal
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public AddressDTO? Address { get; set; }
    public string? PhotoUrl { get; set; }

    public bool? IsActive { get; set; }
    public ICollection<Guid>? RoleIds { get; set; }
}
