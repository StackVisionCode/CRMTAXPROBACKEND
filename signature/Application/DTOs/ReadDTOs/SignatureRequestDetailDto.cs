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
    public IReadOnlyList<SignerSummaryDto> Signers { get; set; } = new List<SignerSummaryDto>();
}
