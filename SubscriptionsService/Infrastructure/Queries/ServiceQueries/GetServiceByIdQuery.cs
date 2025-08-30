using Common;
using DTOs.ServiceDTOs;
using MediatR;

namespace Queries.ServiceQueries;

/// <summary>
/// Obtener Service por ID con módulos incluidos
/// </summary>
public record GetServiceByIdQuery(Guid ServiceId) : IRequest<ApiResponse<ServiceDTO>>;
