using Common;
using CustomerService.DTOs.AddressDTOs;
using MediatR;

namespace CustomerService.Queries.AddressQueries;

public class GetAllAddressQueries : IRequest<ApiResponse<List<ReadAddressDTO>>>;