namespace AuthService.DTOs.InvitationDTOs;

/// <summary>
/// DTO con estadísticas de invitaciones de una company
/// </summary>
public class InvitationStatsDTO
{
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyDomain { get; set; }

    // Límites del plan
    public int CustomPlanUserLimit { get; set; }
    public int CurrentActiveUsers { get; set; }

    // Estadísticas de invitaciones
    public int TotalInvitationsSent { get; set; }
    public int PendingInvitations { get; set; }
    public int AcceptedInvitations { get; set; }
    public int CancelledInvitations { get; set; }
    public int ExpiredInvitations { get; set; }
    public int FailedInvitations { get; set; }

    // Estadísticas por período
    public int InvitationsLast24Hours { get; set; }
    public int InvitationsLast7Days { get; set; }
    public int InvitationsLast30Days { get; set; }

    // Top usuarios que más invitan
    public List<InviterStats> TopInviters { get; set; } = new List<InviterStats>();
    public int AvailableSlots => Math.Max(0, CustomPlanUserLimit - CurrentActiveUsers);
    public int RemainingInvitationsAllowed => Math.Max(0, AvailableSlots - PendingInvitations);
    public bool CanSendMoreInvitations => RemainingInvitationsAllowed > 0;

    // Tasa de aceptación
    public decimal AcceptanceRate =>
        TotalInvitationsSent > 0 ? (decimal)AcceptedInvitations / TotalInvitationsSent * 100 : 0;
}

/// <summary>
/// Estadísticas por usuario que envía invitaciones
/// </summary>
public class InviterStats
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserLastName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public bool IsOwner { get; set; }

    public int TotalInvitationsSent { get; set; }
    public int AcceptedInvitations { get; set; }
    public int PendingInvitations { get; set; }
    public int CancelledInvitations { get; set; }

    public decimal PersonalAcceptanceRate =>
        TotalInvitationsSent > 0 ? (decimal)AcceptedInvitations / TotalInvitationsSent * 100 : 0;
}
