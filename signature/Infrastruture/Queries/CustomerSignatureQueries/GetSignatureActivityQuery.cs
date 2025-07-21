using Application.DTOs.CustomerSignatureDTOs;
using Application.Helpers;
using MediatR;

namespace Infrastructure.Queries.CustomerSignatureQueries;

/// <summary>
/// Query para obtener actividad de una solicitud específica
/// </summary>
public record GetSignatureActivityQuery(Guid RequestId)
    : IRequest<ApiResponse<List<SignatureActivityDto>>>;
