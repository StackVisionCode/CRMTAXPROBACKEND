using Common;
using CustomerService.DTOs.CustomerDTOs;
using MediatR;

namespace CustomerService.Queries.CustomerQueries;

/// <summary>
/// Obtiene todos los customers de una company específica
/// </summary>
public record class GetCustomersByCompanyQueries(Guid CompanyId)
    : IRequest<ApiResponse<List<ReadCustomerDTO>>>;
