using System.ComponentModel.DataAnnotations;
using Applications.DTOs.CompanyDTOs;

namespace AuthService.Applications.DTOs.CompanyDTOs;

public class NewCompanyDTO
{
    public required bool IsCompany { get; set; }

    public string? FullName { get; set; }
    public string? CompanyName { get; set; }
    public AddressDTO? Address { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }

    [Range(0, int.MaxValue)]
    public int UserLimit { get; set; } = 1; // Default 1 para individuales
    public required string Domain { get; set; } = default!;

    public string? Brand { get; set; }

    // Alta del Admin (dueño)
    [EmailAddress]
    public required string Email { get; set; } = default!;

    [MinLength(8)]
    public required string Password { get; set; } = default!;
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public AddressDTO? AdminAddress { get; set; }
    public string? PhotoUrl { get; set; }
}
