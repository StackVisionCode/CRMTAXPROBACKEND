using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.ReadDTOs;

public class SignatureRequestDetailDto
{
    [Key]
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public SignatureStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CreatedByTaxUserId { get; set; }
    public Guid? LastModifiedByTaxUserId { get; set; }
    public IReadOnlyList<SignerSummaryDto> Signers { get; set; } = new List<SignerSummaryDto>();
    public DateTime? RejectedAtUtc { get; set; }
    public string? RejectReason { get; set; }
    public Guid? RejectedBySignerId { get; set; }
    public bool IsRejected => Status == SignatureStatus.Rejected;
}
