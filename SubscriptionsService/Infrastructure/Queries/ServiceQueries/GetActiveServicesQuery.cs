using Common;
using DTOs.ServiceDTOs;
using MediatR;

namespace Queries.ServiceQueries;

/// <summary>
/// Obtener Services activos para selección en front
/// </summary>
public record GetActiveServicesQuery() : IRequest<ApiResponse<IEnumerable<ServiceDTO>>>;
