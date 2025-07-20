namespace Application.DTOs.ReadDTOs;

public class SignerStatusDto
{
    /// <summary>
    /// ID del firmante
    /// </summary>
    public Guid SignerId { get; set; }

    /// <summary>
    /// Email del firmante (para confirmación visual)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Estado actual del firmante
    /// </summary>
    public SignerStatus Status { get; set; }

    /// <summary>
    /// Estado de la solicitud de firma
    /// </summary>
    public SignatureStatus RequestStatus { get; set; }

    /// <summary>
    /// Indica si ya completó el proceso (firmado o rechazado)
    /// </summary>
    public bool IsProcessCompleted { get; set; }

    /// <summary>
    /// Fecha de cuando firmó (si aplica)
    /// </summary>
    public DateTime? SignedAtUtc { get; set; }

    /// <summary>
    /// Fecha de cuando rechazó (si aplica)
    /// </summary>
    public DateTime? RejectedAtUtc { get; set; }

    /// <summary>
    /// Razón del rechazo (si aplica)
    /// </summary>
    public string? RejectReason { get; set; }

    /// <summary>
    /// Nombre completo del firmante
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Orden del firmante en el proceso
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Total de firmantes en la solicitud
    /// </summary>
    public int TotalSigners { get; set; }

    /// <summary>
    /// Firmantes que ya completaron el proceso
    /// </summary>
    public int CompletedSigners { get; set; }

    /// <summary>
    /// Indica si puede proceder (token válido y proceso no completado)
    /// </summary>
    public bool CanProceed { get; set; }

    /// <summary>
    /// Mensaje descriptivo del estado
    /// </summary>
    public string StatusMessage { get; set; } = string.Empty;
}
