using Application.Helpers;
using MediatR;

namespace signature.Application.Commands;

// Command para registrar acceso al preview
public record RecordPreviewAccessCommand(
    string AccessToken,
    string SessionId,
    string? ClientIp,
    string? UserAgent
) : IRequest<ApiResponse<bool>>;
