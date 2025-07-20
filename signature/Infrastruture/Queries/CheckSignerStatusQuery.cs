using Application.DTOs.ReadDTOs;
using Application.Helpers;
using MediatR;

namespace Infrastruture.Queries;

/// <summary>
/// Query para verificar el estado de un firmante usando su token
/// Esta consulta es pública y segura, solo revela el estado necesario
/// </summary>
public sealed record CheckSignerStatusQuery(string Token) : IRequest<ApiResponse<SignerStatusDto>>;
