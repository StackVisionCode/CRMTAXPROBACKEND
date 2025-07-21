using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.CustomerSignatureDTOs;

/// <summary>
/// DTO para solicitudes de firma de un cliente específico
/// </summary>
public class CustomerSignatureRequestDto
{
    [Key]
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public SignatureStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int TotalSigners { get; set; }
    public int CompletedSigners { get; set; }
    public double Progress => TotalSigners > 0 ? (CompletedSigners * 100.0) / TotalSigners : 0;
    public List<CustomerSignerDto> Signers { get; set; } = new();
    public string? EstimatedCompletion { get; set; }
    public string LastActivity =>
        UpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        ?? CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    public Guid? RejectedBySignerId { get; set; }
    public string? RejectReason { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
}
