using System.ComponentModel.DataAnnotations;
using Applications.DTOs.CompanyDTOs;

namespace AuthService.DTOs.UserCompanyDTOs;

public class RegisterByInvitationDTO
{
    public required string InvitationToken { get; set; }

    [MinLength(6)]
    public required string Password { get; set; }

    public string? Name { get; set; }
    public string? LastName { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }

    public AddressDTO? Address { get; set; }
    public string? PhotoUrl { get; set; }
}
