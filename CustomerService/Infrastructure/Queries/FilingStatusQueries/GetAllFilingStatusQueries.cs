using Common;
using CustomerService.DTOs.FilingStatusDTOs;
using MediatR;

namespace CustomerService.Queries.FilingStatusQueries;

public record class GetAllFilingStatusQueries : IRequest<ApiResponse<List<ReadFilingStatusDto>>>;
