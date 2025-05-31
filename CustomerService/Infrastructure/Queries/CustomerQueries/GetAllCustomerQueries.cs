using Common;
using CustomerService.DTOs.CustomerDTOs;
using MediatR;

namespace CustomerService.Queries.CustomerQueries;

public record class GetAllCustomerQueries : IRequest<ApiResponse<List<ReadCustomerDTO>>>;
