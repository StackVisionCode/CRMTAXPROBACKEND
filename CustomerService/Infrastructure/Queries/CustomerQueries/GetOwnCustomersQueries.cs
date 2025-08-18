using Common;
using CustomerService.DTOs.CustomerDTOs;
using MediatR;

namespace CustomerService.Queries.CustomerQueries;

/// <summary>
/// Devuelve los customers filtrados por company y opcionalmente por creador
/// </summary>
public sealed record GetOwnCustomersQueries(Guid CompanyId, Guid? CreatedByTaxUserId = null)
    : IRequest<ApiResponse<List<ReadCustomerDTO>>>;
