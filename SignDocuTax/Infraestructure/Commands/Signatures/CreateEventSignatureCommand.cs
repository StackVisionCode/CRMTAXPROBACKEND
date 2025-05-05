using Common;
using Domain.Signatures;
using MediatR;

public record class CreateEventSignatureCommand (EventSignature EventSign)  : IRequest<ApiResponse<bool>>;