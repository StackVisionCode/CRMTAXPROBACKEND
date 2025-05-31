using Common;
using CustomerService.DTOs.PreferredContactDTOs;
using MediatR;

namespace CustomerService.Queries.PreferredContactQueries;

public record class GetByIdPreferredContactQueries(Guid Id)
    : IRequest<ApiResponse<ReadPreferredContactDTO>>;
