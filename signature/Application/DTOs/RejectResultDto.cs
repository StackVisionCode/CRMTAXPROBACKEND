namespace signature.Application.DTOs;

public class RejectResultDto
{
    public Guid SignatureRequestId { get; set; }
    public Guid SignerId { get; set; }
    public SignatureStatus RequestStatus { get; set; }
    public SignerStatus SignerStatus { get; set; }
    public DateTime RejectedAtUtc { get; set; }
    public string? Reason { get; set; }
}
