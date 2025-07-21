using Application.DTOs.CustomerSignatureDTOs;
using Application.Helpers;
using MediatR;

namespace Infrastructure.Queries.CustomerSignatureQueries;

/// <summary>
/// Query para obtener solicitudes urgentes de un cliente
/// </summary>
public record GetUrgentSignatureRequestsQuery(Guid CustomerId)
    : IRequest<ApiResponse<List<CustomerSignatureRequestDto>>>;
