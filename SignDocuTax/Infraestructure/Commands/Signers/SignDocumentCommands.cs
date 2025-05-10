using Common;
using MediatR;

namespace Commands.Signers;

public record class SignDocumentCommands(int ExternalSignerId) : IRequest<ApiResponse<bool>>;
