using Common;
using CustomerService.DTOs.CustomerDTOs;
using MediatR;

namespace CustomerService.Queries.CustomerTypeQueries;

public record class GetAllCustomerTypeQueries : IRequest<ApiResponse<List<CustomerTypeDTO>>>;