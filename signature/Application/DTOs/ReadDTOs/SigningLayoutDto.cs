using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.ReadDTOs;

public enum BoxKind
{
    Signature = 0,
    Initials = 1,
    Date = 2,
}

public class SigningBoxDto
{
    [Key]
    public Guid BoxId { get; set; }
    public int Page { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public BoxKind Kind { get; set; }
    public Guid SignerId { get; set; }
    public int SignerOrder { get; set; }
    public SignerStatus SignerStatus { get; set; }
    public bool IsCurrentSigner { get; set; }
    public DateTime? SignedAtUtc { get; set; }
    public string? InitialsValue { get; set; }
    public string? DateValue { get; set; }
}

public class SigningLayoutDto
{
    public Guid SignatureRequestId { get; set; }
    public Guid DocumentId { get; set; }
    public SignatureStatus RequestStatus { get; set; }
    public Guid CurrentSignerId { get; set; }
    public int CurrentSignerOrder { get; set; }
    public IReadOnlyList<SigningBoxDto> Boxes { get; set; } = new List<SigningBoxDto>();
}
