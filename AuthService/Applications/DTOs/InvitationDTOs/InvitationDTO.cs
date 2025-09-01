using System.ComponentModel.DataAnnotations;
using AuthService.Applications.Common;

namespace AuthService.DTOs.InvitationDTOs;

/// <summary>
/// DTO para obtener invitaciones con información completa
/// </summary>
public class InvitationDTO
{
    [Key]
    public Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid InvitedByUserId { get; set; }
    public required string Email { get; set; }
    public required string Token { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public InvitationStatus Status { get; set; }
    public string? PersonalMessage { get; set; }
    public List<Guid> RoleIds { get; set; } = new List<Guid>();

    // Auditoría
    public DateTime CreatedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public string? CancellationReason { get; set; }
    public Guid? RegisteredUserId { get; set; }

    // Metadata
    public string? InvitationLink { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Información del usuario que invitó
    public string InvitedByUserName { get; set; } = string.Empty;
    public string InvitedByUserLastName { get; set; } = string.Empty;
    public string InvitedByUserEmail { get; set; } = string.Empty;
    public bool InvitedByUserIsOwner { get; set; }

    // Información del usuario que canceló (si aplica)
    public string? CancelledByUserName { get; set; }
    public string? CancelledByUserLastName { get; set; }
    public string? CancelledByUserEmail { get; set; }

    // Información del usuario registrado (si aplica)
    public string? RegisteredUserName { get; set; }
    public string? RegisteredUserLastName { get; set; }
    public string? RegisteredUserEmail { get; set; }

    // Información de la company
    public string? CompanyName { get; set; }
    public string? CompanyFullName { get; set; }
    public string? CompanyDomain { get; set; }
    public bool CompanyIsCompany { get; set; }
    public ServiceLevel CompanyServiceLevel { get; set; }

    // Nombres de roles asignados
    public List<string> RoleNames { get; set; } = new List<string>();

    // Estados calculados
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsActive => Status == InvitationStatus.Pending && !IsExpired;
    public bool CanBeCancelled => Status == InvitationStatus.Pending && !IsExpired;

    // Tiempo restante
    public TimeSpan? TimeRemaining => IsActive ? ExpiresAt - DateTime.UtcNow : null;
    public string? TimeRemainingText =>
        TimeRemaining?.TotalDays >= 1
            ? $"{(int)TimeRemaining.Value.TotalDays}d {TimeRemaining.Value.Hours}h"
        : TimeRemaining?.TotalHours >= 1
            ? $"{(int)TimeRemaining.Value.TotalHours}h {TimeRemaining.Value.Minutes}m"
        : TimeRemaining?.TotalMinutes > 0 ? $"{(int)TimeRemaining.Value.TotalMinutes}m"
        : null;
}
