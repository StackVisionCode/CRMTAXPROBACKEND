using Common;
using CustomerService.DTOs.AddressDTOs;
using MediatR;

namespace CustomerService.Queries.AddressQueries;

public record class GetByIdAddressQueries(Guid Id) : IRequest<ApiResponse<ReadAddressDTO>>;
