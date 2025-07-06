using Common;
using CustomerService.DTOs.ContactInfoDTOs;
using MediatR;

namespace CustomerService.Queries.ContactInfoQueries;

public record class GetContactInfoByCustomerIdQueries(Guid CustomerId)
    : IRequest<ApiResponse<ReadContactInfoDTO>>;
