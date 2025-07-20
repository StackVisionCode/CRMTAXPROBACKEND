using Application.Helpers;
using MediatR;

namespace signature.Application.Commands;

// Command para invalidar acceso al preview
public record InvalidatePreviewAccessCommand(string AccessToken, string SessionId)
    : IRequest<ApiResponse<bool>>;
