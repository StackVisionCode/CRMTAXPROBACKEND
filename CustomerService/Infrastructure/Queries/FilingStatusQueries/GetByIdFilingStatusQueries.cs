using CustomerService.DTOs.FilingStatusDTOs;
using MediatR;

namespace CustomerService.Queries.FilingStatusQueries;

public record class GetByIdFilingStatusQueries(Guid Id)
    : IRequest<Common.ApiResponse<ReadFilingStatusDto>>;
