using System.ComponentModel.DataAnnotations;
using Applications.DTOs.CompanyDTOs;

namespace AuthService.DTOs.UserCompanyDTOs;

public class NewUserCompanyDTO
{
    public required Guid CompanyId { get; set; }

    [EmailAddress]
    public required string Email { get; set; }

    [MinLength(6)]
    public required string Password { get; set; }
    public string? Name { get; set; }
    public string? LastName { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }
    public string? PhotoUrl { get; set; }
    public AddressDTO? Address { get; set; }
    public ICollection<Guid>? RoleIds { get; set; }
}
