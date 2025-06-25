using Common;
using CustomerService.DTOs.ContactInfoDTOs;
using MediatR;

namespace CustomerService.Queries.ContactInfoQueries;

public record class GetCustomerProfileQuery(Guid CustomerId)
    : IRequest<ApiResponse<CustomerProfileDTO>>;
