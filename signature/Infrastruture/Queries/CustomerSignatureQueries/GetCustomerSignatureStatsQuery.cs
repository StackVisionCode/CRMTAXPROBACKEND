using Application.DTOs.CustomerSignatureDTOs;
using Application.Helpers;
using MediatR;

namespace Infrastructure.Queries.CustomerSignatureQueries;

/// <summary>
/// Query para obtener estadísticas de un cliente
/// </summary>
public record GetCustomerSignatureStatsQuery(Guid CustomerId)
    : IRequest<ApiResponse<CustomerSignatureStatsDto>>;
