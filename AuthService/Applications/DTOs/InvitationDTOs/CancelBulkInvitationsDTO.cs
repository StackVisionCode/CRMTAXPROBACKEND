using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.InvitationDTOs;

/// <summary>
/// DTO para cancelar múltiples invitaciones
/// </summary>
public class CancelBulkInvitationsDTO
{
    [Required]
    public required List<Guid> InvitationIds { get; set; }

    public string? CancellationReason { get; set; }
}
