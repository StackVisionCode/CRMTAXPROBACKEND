using System.ComponentModel.DataAnnotations;

namespace signature.Application.DTOs;

public class SignerInfoDto
{
    public Guid? CustomerId { get; set; }

    [EmailAddress]
    public required string Email { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El orden debe ser mayor a 0")]
    public int Order { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El número de página debe ser mayor a 0")]
    public int Page { get; set; }

    [Range(0, float.MaxValue)]
    public float PosX { get; set; }

    [Range(0, float.MaxValue)]
    public float PosY { get; set; }

    public float Width { get; set; } // en puntos PDF
    public float Height { get; set; } // en puntos PDF

    public InitialEntityDto? InitialEntity { get; set; }
    public SignerStatus Status { get; set; }
    public FechaSignerDto? FechaSigner { get; set; }
}
