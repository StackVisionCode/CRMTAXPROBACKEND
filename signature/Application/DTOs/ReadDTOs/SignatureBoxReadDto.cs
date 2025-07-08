using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.ReadDTOs;

public class SignatureBoxReadDto
{
    [Key]
    public Guid Id { get; set; }
    public int Page { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public string? Initials { get; set; } // null cuando no aplica
    public string? DateText { get; set; } // idem
}
