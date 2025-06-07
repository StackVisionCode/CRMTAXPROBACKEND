using Common;
using CustomerService.DTOs.CustomerDTOs;
using MediatR;

namespace CustomerService.Queries.CustomerQueries;

/// <summary>
/// Devuelve los clientes que pertenecen al usuario logueado
///   – Si companyId ≠ Guid.Empty   ⇒  todos los clientes creados por cualquier usuario de la compañía.
///   – Si companyId == Guid.Empty ⇒  sólo los que creó el propio usuario.
/// </summary>
public sealed record GetOwnCustomersQueries(Guid UserId)
    : IRequest<ApiResponse<List<ReadCustomerDTO>>>;
