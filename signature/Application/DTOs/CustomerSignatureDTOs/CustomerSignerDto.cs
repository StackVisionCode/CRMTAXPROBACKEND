namespace Application.DTOs.CustomerSignatureDTOs;

/// <summary>
/// DTO para firmantes en el contexto de cliente
/// </summary>
public class CustomerSignerDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// SignerId mapeado correctamente para el frontend
    /// </summary>
    public Guid SignerId => Id;

    public Guid? CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public SignerStatus Status { get; set; }
    public int Order { get; set; }
    public DateTime? SignedAtUtc { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public string? RejectReason { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public int AccessCount { get; set; }
    public int RemindersSent { get; set; }
    public DateTime CreatedAt { get; set; }

    // Propiedades calculadas para el frontend
    public bool IsCurrentTurn { get; set; }
    public int? TimeToSign { get; set; } // en horas
}
