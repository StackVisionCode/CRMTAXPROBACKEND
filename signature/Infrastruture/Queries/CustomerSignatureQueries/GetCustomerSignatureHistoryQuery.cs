using Application.DTOs.CustomerSignatureDTOs;
using Application.Helpers;
using MediatR;

namespace Infrastructure.Queries.CustomerSignatureQueries;

/// <summary>
/// Query para obtener historial completo de un cliente
/// </summary>
public record GetCustomerSignatureHistoryQuery(Guid CustomerId)
    : IRequest<ApiResponse<List<CustomerSignatureRequestDto>>>;
