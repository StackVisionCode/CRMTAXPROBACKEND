using Common;
using CustomerService.DTOs.PreferredContactDTOs;
using MediatR;

namespace CustomerService.Queries.PreferredContactQueries;

public record class GetAllPreferredContactQueries
    : IRequest<ApiResponse<List<ReadPreferredContactDTO>>>;
