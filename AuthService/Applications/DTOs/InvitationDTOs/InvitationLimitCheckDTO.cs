using AuthService.Applications.Common;

namespace AuthService.DTOs.InvitationDTOs;

/// <summary>
/// DTO para verificación de límites de invitaciones
/// </summary>
public class InvitationLimitCheckDTO
{
    public required Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public ServiceLevel ServiceLevel { get; set; }
    public bool IsCompany { get; set; }

    // Estadísticas de usuarios (disponibles en AuthService)
    public int CurrentActiveUsers { get; set; }
    public int CurrentTotalUsers { get; set; }
    public int OwnerCount { get; set; }

    // Estadísticas de invitaciones (disponibles en AuthService)
    public int PendingInvitations { get; set; }
    public int ExpiredInvitations { get; set; }
    public int AcceptedInvitations { get; set; }
    public int TotalInvitationsSent { get; set; }

    // Validación básica de AuthService
    public bool CanSendBasicValidation { get; set; }
    public string BasicValidationMessage { get; set; } = string.Empty;

    // Metadata
    public bool RequiresSubscriptionCheck { get; set; } = true;
    public DateTime LastCheckedAt { get; set; }

    // Propiedades calculadas para el frontend
    public bool HasActiveOwner => OwnerCount > 0;
    public int InactiveUsers => CurrentTotalUsers - CurrentActiveUsers;
    public double InvitationSuccessRate =>
        TotalInvitationsSent > 0 ? (double)AcceptedInvitations / TotalInvitationsSent * 100 : 0;
}
