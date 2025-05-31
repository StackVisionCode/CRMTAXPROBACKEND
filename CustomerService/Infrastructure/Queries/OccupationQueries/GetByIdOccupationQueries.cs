using Common;
using CustomerService.DTOs.OccupationDTOs;
using MediatR;

namespace CustomerService.Queries.OccupationQueries;

public record class GetByIdOccupationQueries(Guid Id) : IRequest<ApiResponse<ReadOccupationDTO>>;
