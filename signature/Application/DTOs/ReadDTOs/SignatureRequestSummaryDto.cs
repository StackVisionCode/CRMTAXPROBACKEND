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
}
