using Common;
using CustomerService.DTOs.AddressDTOs;
using MediatR;

namespace CustomerService.Queries.AddressQueries;

public record class GetAddressByCustomerIdQueries(Guid CustomerId)
    : IRequest<ApiResponse<ReadAddressDTO>>;
