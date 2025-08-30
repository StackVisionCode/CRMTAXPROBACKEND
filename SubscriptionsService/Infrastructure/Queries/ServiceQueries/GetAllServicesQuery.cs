using Common;
using DTOs.ServiceDTOs;
using MediatR;

namespace Queries.ServiceQueries;

/// <summary>
/// Obtener todos los Services (Solo Developer)
/// Incluye activos e inactivos
/// </summary>
public record GetAllServicesQuery(bool? IsActive = null)
    : IRequest<ApiResponse<IEnumerable<ServiceDTO>>>;
