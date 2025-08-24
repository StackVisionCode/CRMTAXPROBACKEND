using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.InvitationDTOs;

/// <summary>
/// DTO para verificación de límites de invitaciones
/// </summary>
public class InvitationLimitCheckDTO
{
    public Guid CompanyId { get; set; }
    public bool CanSendMore { get; set; }
    public int CurrentActiveUsers { get; set; }
    public int CustomPlanUserLimit { get; set; }
    public int PendingInvitations { get; set; }
    public int AvailableSlots { get; set; }
    public int RemainingInvitationsAllowed { get; set; }
    public string? LimitMessage { get; set; }
}
