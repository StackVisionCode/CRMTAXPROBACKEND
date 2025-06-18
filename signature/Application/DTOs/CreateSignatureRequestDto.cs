using System.ComponentModel.DataAnnotations;

namespace signature.Application.DTOs;

public class CreateSignatureRequestDto
{
    public Guid Id { get; set; }
    public required Guid DocumentId { get; set; }

    [MinLength(1, ErrorMessage = "Al menos un firmante es requerido")]
    public required IReadOnlyList<SignerInfoDto> Signers { get; set; } = [];
}
