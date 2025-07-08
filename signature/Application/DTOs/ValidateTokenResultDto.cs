namespace signature.Application.DTOs;

public class ValidateTokenResultDto
{
    public Guid SignatureRequestId { get; set; }
    public SignerInfoDto? Signer { get; set; }
    public Guid DocumentId { get; set; }
    public SignatureStatus RequestStatus { get; set; }
    public string DocumentAccessToken { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
}
