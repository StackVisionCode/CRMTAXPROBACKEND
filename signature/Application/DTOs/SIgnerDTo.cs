// DTOs
using Application.Helpers;
using MediatR;

public record SignerInfo(Guid CustomerId, string Email, int Order);
public record CreateSignatureRequestDto(Guid DocumentId, IReadOnlyList<SignerInfo> Signers);

public record SignDocumentDto(string Token,string SignatureImageBase64,DigitalCertificateDto Certificate);

public record DigitalCertificateDto(string Thumbprint, string Subject, DateTime NotBefore, DateTime NotAfter);

// Commands / Queries
public record CreateSignatureRequestCommand(CreateSignatureRequestDto Payload): IRequest<ApiResponse<bool>>;

public record ValidateTokenQuery(string Token): IRequest<ApiResponse<bool>>;

public record SubmitSignatureCommand(SignDocumentDto Payload): IRequest<ApiResponse<bool>>;
