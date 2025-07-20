using Application.DTOs;
using Application.Helpers;
using MediatR;

namespace signature.Application.Queries;

// Query para obtener información del preview
public record GetPreviewInfoQuery(string AccessToken, string SessionId)
    : IRequest<ApiResponse<DocumentPreviewInfoDto>>;
