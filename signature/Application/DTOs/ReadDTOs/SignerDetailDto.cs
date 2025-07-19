using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.ReadDTOs;

public class SignerDetailDto
{
    [Key]
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public int Order { get; set; }
    public SignerStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SignedAtUtc { get; set; }
    public string RejectedReason { get; set; } = string.Empty;
    public DateTime? RejectedAtUtc { get; set; }
    public string? FullName { get; set; }
    public IReadOnlyList<SignatureBoxReadDto> Boxes { get; set; } = new List<SignatureBoxReadDto>();
}
