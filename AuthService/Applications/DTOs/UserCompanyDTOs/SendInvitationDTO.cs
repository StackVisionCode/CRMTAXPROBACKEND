using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.UserCompanyDTOs;

public class SendInvitationDTO
{
    public required Guid CompanyId { get; set; }

    [EmailAddress]
    public required string Email { get; set; }

    [MaxLength(500)]
    public string? PersonalMessage { get; set; }
    public ICollection<Guid>? RoleIds { get; set; }
}
