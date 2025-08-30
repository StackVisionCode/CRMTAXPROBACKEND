using AuthService.Domains.Companies;
using AuthService.Domains.Users;
using Common;

namespace AuthService.Domains.Invitations;

/// <summary>
/// Registro de invitaciones enviadas por companies a usuarios
/// Permite auditoría, control de límites y gestión de estados
/// </summary>
public class Invitation : BaseEntity
{
    public required Guid CompanyId { get; set; }
    public virtual Company Company { get; set; } = null!;

    public required Guid InvitedByUserId { get; set; } // TaxUser que envía la invitación (Owner/Administrator)
    public virtual TaxUser InvitedByUser { get; set; } = null!;

    public required string Email { get; set; } // Email del usuario invitado
    public required string Token { get; set; } // Token de invitación generado
    public required DateTime ExpiresAt { get; set; } // Cuándo expira el token

    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public string? PersonalMessage { get; set; }

    // Roles asignados en la invitación (JSON serializado)
    public List<Guid> RoleIds { get; set; } = new List<Guid>();

    // Auditoría y seguimiento
    public DateTime? AcceptedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledByUserId { get; set; } // Quién canceló la invitación
    public virtual TaxUser? CancelledByUser { get; set; }
    public string? CancellationReason { get; set; }

    // Usuario registrado (si la invitación fue aceptada)
    public Guid? RegisteredUserId { get; set; }
    public virtual TaxUser? RegisteredUser { get; set; }

    // Metadata adicional
    public string? InvitationLink { get; set; } // Link completo enviado
    public string? IpAddress { get; set; } // IP desde donde se envió
    public string? UserAgent { get; set; } // Browser/dispositivo

    // Estado calculado
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsActive => Status == InvitationStatus.Pending && !IsExpired;
    public bool CanBeCancelled => Status == InvitationStatus.Pending && !IsExpired;
}
