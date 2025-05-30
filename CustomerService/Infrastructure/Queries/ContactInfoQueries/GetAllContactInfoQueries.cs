using Common;
using CustomerService.DTOs.ContactInfoDTOs;
using MediatR;

namespace CustomerService.Queries.ContactInfoQueries;

public record class GetAllContactInfoQueries : IRequest<ApiResponse<List<ReadContactInfoDTO>>>;