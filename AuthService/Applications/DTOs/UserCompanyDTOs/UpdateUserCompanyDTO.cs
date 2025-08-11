using System.ComponentModel.DataAnnotations;
using Applications.DTOs.CompanyDTOs;

namespace AuthService.DTOs.UserCompanyDTOs;

public class UpdateUserCompanyDTO
{
    public required Guid Id { get; set; }
    public string? Name { get; set; }
    public string? LastName { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; }
    public AddressDTO? Address { get; set; }
    public ICollection<Guid>? RoleIds { get; set; }
}
