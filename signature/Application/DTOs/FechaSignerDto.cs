namespace signature.Application.DTOs;
public class FechaSignerDto
{
    public string FechaValue { get; set; } = string.Empty;
    public float PositionYFechaSigner { get; set; }
    public float PositionXFechaSigner { get; set; }
    public float WidthFechaSigner { get; set; } // en puntos PDF
    public float HeightFechaSigner { get; set; } // en puntos PDF
}