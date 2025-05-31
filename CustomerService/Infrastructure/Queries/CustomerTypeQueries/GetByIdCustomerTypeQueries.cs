using Common;
using CustomerService.DTOs.CustomerDTOs;
using MediatR;

namespace CustomerService.Queries.CustomerTypeQueries;

public record class GetByIdCustomerTypeQueries(Guid Id) : IRequest<ApiResponse<ReadCustomerTypeDTO>>;