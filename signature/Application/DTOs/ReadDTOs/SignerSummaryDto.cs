using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.ReadDTOs;

public class SignerSummaryDto
{
    [Key]
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public int Order { get; set; }
    public SignerStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SignedAtUtc { get; set; }
}
