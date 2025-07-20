namespace Application.DTOs.ReadDTOs;

public class SignerListItemDto
{
    public Guid Id { get; set; }
    public Guid SignatureRequestId { get; set; }
    public Guid DocumentId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public int Order { get; set; }
    public SignerStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SignedAtUtc { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public string? RejectedReason { get; set; }
    public SignatureStatus RequestStatus { get; set; }
    public DateTime RequestCreatedAt { get; set; }
    public DateTime? RequestUpdatedAt { get; set; }
    public bool RequestIsRejected => RequestStatus == SignatureStatus.Rejected;
    public int BoxesCount { get; set; }
    public int SignatureBoxesCount { get; set; }
    public int InitialsBoxesCount { get; set; }
    public int DateBoxesCount { get; set; }
}
