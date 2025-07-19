using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.ReadDTOs;

public class SignatureRequestSummaryDto
{
    [Key]
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public SignatureStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int SignerCount { get; set; }
    public int SignedCount { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public string? RejectReason { get; set; }
    public Guid? RejectedBySignerId { get; set; }
    public bool IsRejected => Status == SignatureStatus.Rejected;
}
