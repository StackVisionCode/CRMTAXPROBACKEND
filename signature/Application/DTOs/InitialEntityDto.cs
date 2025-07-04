namespace signature.Application.DTOs;

public class InitialEntityDto
{
    public string InitalValue { get; set; } = string.Empty;

    public float PositionYIntial { get; set; }

    public float PositionXIntial { get; set; }

    public float WidthIntial { get; set; } // en puntos PDF
    public float HeightIntial { get; set; } // en puntos PDF
}
