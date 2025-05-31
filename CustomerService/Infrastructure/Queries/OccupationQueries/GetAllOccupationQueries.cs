using Common;
using CustomerService.DTOs.OccupationDTOs;
using MediatR;

namespace CustomerService.Queries.OccupationQueries;

public record class GetAllOccupationQueries : IRequest<ApiResponse<List<ReadOccupationDTO>>>;
