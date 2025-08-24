using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.InvitationDTOs;

/// <summary>
/// DTO para cancelar invitaciones
/// </summary>
public class CancelInvitationDTO
{
    [Required]
    public required Guid InvitationId { get; set; }

    public string? CancellationReason { get; set; }
}
