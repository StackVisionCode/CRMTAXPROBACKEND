using System.ComponentModel.DataAnnotations;
using Applications.DTOs.AddressDTOs;

namespace AuthService.Applications.DTOs.CompanyDTOs;

public class CreateUserByDeveloperDTO
{
    [Key]
    public required Guid CompanyId { get; set; }

    [EmailAddress]
    public required string Email { get; set; } = default!;

    [MinLength(8)]
    public required string Password { get; set; } = default!;

    public bool IsOwner { get; set; } = false;
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public AddressDTO? Address { get; set; }
    public string? PhotoUrl { get; set; }
    public ICollection<Guid> RoleIds { get; set; } = new List<Guid>();
    public bool IgnoreUserLimit { get; set; } = false; //Esto permite a developers omitir límites
}
