using Common;
using CustomerService.DTOs.ContactInfoDTOs;
using MediatR;

namespace CustomerService.Queries.ContactInfoQueries;

public record class GetAuthInfoByEmailQuery(string Email) : IRequest<ApiResponse<AuthInfoDTO>>;
