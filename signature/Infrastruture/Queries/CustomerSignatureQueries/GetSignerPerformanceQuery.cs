using Application.DTOs.CustomerSignatureDTOs;
using Application.Helpers;
using MediatR;

namespace Infrastructure.Queries.CustomerSignatureQueries;

/// <summary>
/// Query para obtener rendimiento de firmantes de un cliente
/// </summary>
public record GetSignerPerformanceQuery(Guid CustomerId)
    : IRequest<ApiResponse<List<SignerPerformanceDto>>>;
