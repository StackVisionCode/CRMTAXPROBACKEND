using Application.DTOs;
using Application.Helpers;
using MediatR;

namespace signature.Application.Queries;

// Query para verificar estado del preview
public record CheckPreviewStatusQuery(string AccessToken, string SessionId)
    : IRequest<ApiResponse<DocumentPreviewStatusDto>>;
