using Application.DTOs;
using Application.Helpers;
using MediatR;

namespace signature.Application.Queries;

// Query para verificar si hay preview disponible para un firmante
public record CheckAvailablePreviewQuery(Guid SignerId)
    : IRequest<ApiResponse<AvailablePreviewDto>>;
