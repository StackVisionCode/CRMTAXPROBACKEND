using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.CustomerSignatureDTOs;

/// <summary>
/// DTO para actividad de firmas
/// </summary>
public class SignatureActivityDto
{
    [Key]
    public Guid Id { get; set; }
    public Guid SignatureRequestId { get; set; }
    public Guid? SignerId { get; set; }
    public string? SignerName { get; set; }
    public string? SignerEmail { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public object? Metadata { get; set; }
}
