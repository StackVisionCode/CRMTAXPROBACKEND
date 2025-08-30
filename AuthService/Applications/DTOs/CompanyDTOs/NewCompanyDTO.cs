using System.ComponentModel.DataAnnotations;
using Applications.DTOs.AddressDTOs;
using AuthService.Applications.Common;

namespace AuthService.Applications.DTOs.CompanyDTOs;

public class NewCompanyDTO
{
    public bool IsCompany { get; set; }
    public string? FullName { get; set; }
    public string? CompanyName { get; set; }
    public string? Brand { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }

    public required string Domain { get; set; }
    public AddressDTO? Address { get; set; }
    public ServiceLevel ServiceLevel { get; set; } = ServiceLevel.Basic;

    // Admin user info (será el TaxUser Owner)
    [EmailAddress]
    public required string Email { get; set; }

    [MinLength(6)]
    public required string Password { get; set; }
    public string? Name { get; set; }
    public string? LastName { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }
    public string? PhotoUrl { get; set; }
    public AddressDTO? AdminAddress { get; set; }
}
