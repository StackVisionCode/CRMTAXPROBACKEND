using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.InvitationDTOs;

/// <summary>
/// DTO para crear nuevas invitaciones
/// </summary>
public class NewInvitationDTO
{
    [Required]
    public required Guid CompanyId { get; set; }

    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    public List<Guid>? RoleIds { get; set; }
    public string? PersonalMessage { get; set; }
}
