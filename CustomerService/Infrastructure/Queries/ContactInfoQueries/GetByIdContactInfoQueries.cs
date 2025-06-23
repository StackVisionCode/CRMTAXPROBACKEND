using Common;
using CustomerService.DTOs.ContactInfoDTOs;
using MediatR;

namespace CustomerService.Queries.ContactInfoQueries;

public record class GetByIdContactInfoQueries(Guid Id) : IRequest<ApiResponse<ReadContactInfoDTO>>;
