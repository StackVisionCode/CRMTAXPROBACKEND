using AuthService.Applications.Common;

namespace AuthService.DTOs.InvitationDTOs;

/// <summary>
/// DTO con estadísticas de invitaciones basado en datos de AuthService
/// El frontend debe combinar con datos de SubscriptionsService para límites completos
/// </summary>
public class InvitationStatsDTO
{
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyDomain { get; set; }
    public ServiceLevel ServiceLevel { get; set; }
    public bool IsCompany { get; set; }

    // Estadísticas de usuarios (disponibles en AuthService)
    public int CurrentActiveUsers { get; set; }
    public int CurrentTotalUsers { get; set; }
    public int OwnerCount { get; set; }

    // Estadísticas de invitaciones (disponibles en AuthService)
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

    // Propiedades calculadas con datos de AuthService
    public int InactiveUsers => CurrentTotalUsers - CurrentActiveUsers;
    public decimal AcceptanceRate =>
        TotalInvitationsSent > 0 ? (decimal)AcceptedInvitations / TotalInvitationsSent * 100 : 0;
    public bool HasActiveOwner => OwnerCount > 0;

    // Metadata
    public bool RequiresSubscriptionCheck { get; set; } = true;
    public DateTime GeneratedAt { get; set; }
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

    // Propiedades calculadas
    public string FullName => $"{UserName} {UserLastName}".Trim();
    public decimal PersonalAcceptanceRate =>
        TotalInvitationsSent > 0 ? (decimal)AcceptedInvitations / TotalInvitationsSent * 100 : 0;
}
