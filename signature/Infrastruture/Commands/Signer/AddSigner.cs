using Application.Helpers;
using MediatR;

public record class AddSigner(Guid custId, string email, int order):IRequest<ApiResponse<bool>>;

public record class  ReceiveSignature(Guid signerId, string img, DigitalCertificate cert):IRequest<ApiResponse<bool>>;
