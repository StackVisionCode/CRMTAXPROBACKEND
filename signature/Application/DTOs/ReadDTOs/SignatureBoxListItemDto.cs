namespace Application.DTOs.ReadDTOs;

public class SignatureBoxListItemDto
{
    public Guid Id { get; set; }
    public Guid SignerId { get; set; }
    public Guid SignatureRequestId { get; set; }
    public Guid DocumentId { get; set; }
    public string SignerEmail { get; set; } = string.Empty;
    public string? SignerFullName { get; set; }
    public int SignerOrder { get; set; }
    public SignerStatus SignerStatus { get; set; }
    public SignatureStatus RequestStatus { get; set; }
    public int Page { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public string BoxKind { get; set; } = string.Empty;
    public string? InitialsValue { get; set; }
    public string? DateValue { get; set; }
    public DateTime? SignedAtUtc { get; set; }
}
