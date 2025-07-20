using System.ComponentModel.DataAnnotations;
using signature.Application.DTOs;

namespace Signature.Application.DTOs;

public class SignatureBoxDto
{
    public Guid? SignerId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La página debe iniciar en 1")]
    public int Page { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }

    [Range(1, float.MaxValue)]
    public float Width { get; set; }

    [Range(1, float.MaxValue)]
    public float Height { get; set; }

    public InitialEntityDto? InitialEntity { get; set; }
    public FechaSignerDto? FechaSigner { get; set; }
}
