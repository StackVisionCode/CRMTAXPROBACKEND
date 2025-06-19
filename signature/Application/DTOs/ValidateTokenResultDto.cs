namespace signature.Application.DTOs;

public class ValidateTokenResultDto
{
    public Guid SignatureRequestId { get; set; }
    public Guid SignerId { get; set; }
    public Guid DocumentId { get; set; }
    public string? SignerEmail { get; set; }
    public SignerStatus SignerStatus { get; set; }
    public SignatureStatus RequestStatus { get; set; }
}
