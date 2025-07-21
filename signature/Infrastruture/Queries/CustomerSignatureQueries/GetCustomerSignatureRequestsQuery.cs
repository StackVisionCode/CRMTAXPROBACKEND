using Application.DTOs.CustomerSignatureDTOs;
using Application.Helpers;
using MediatR;

namespace Infrastructure.Queries.CustomerSignatureQueries;

/// <summary>
/// Query para obtener solicitudes de firma de un cliente
/// </summary>
public record GetCustomerSignatureRequestsQuery(Guid CustomerId)
    : IRequest<ApiResponse<List<CustomerSignatureRequestDto>>>;
