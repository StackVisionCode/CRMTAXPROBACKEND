using Common;
using CustomerService.DTOs.CustomerDTOs;
using MediatR;

namespace CustomerService.Queries.CustomerQueries;

public record class GetByIdCustomerQueries(Guid Id) : IRequest<ApiResponse<ReadCustomerDTO>>;
