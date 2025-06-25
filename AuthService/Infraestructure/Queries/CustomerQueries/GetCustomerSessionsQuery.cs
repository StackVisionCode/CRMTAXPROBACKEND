using AuthService.DTOs.SessionDTOs;
using Common;
using MediatR;

namespace Queries.CustomerQueries;

public record class GetCustomerSessionsQuery(Guid CustomerId)
    : IRequest<ApiResponse<List<ReadCustomerSessionDTO>>>;
