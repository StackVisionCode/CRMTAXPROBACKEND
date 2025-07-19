using System.ComponentModel.DataAnnotations;
using Signature.Application.DTOs;

namespace signature.Application.DTOs;

public class SignerInfoDto
{
    public Guid? CustomerId { get; set; }

    [EmailAddress]
    public required string Email { get; set; }

    [Range(1, int.MaxValue)]
    public int Order { get; set; }
    public SignerStatus Status { get; set; }

    [MaxLength(150)]
    public string? FullName { get; set; }

    [MinLength(1, ErrorMessage = "Se requiere al menos una posición de firma")]
    public required IReadOnlyList<SignatureBoxDto> Boxes { get; set; } = [];
}
