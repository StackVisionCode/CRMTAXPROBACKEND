using Common;
using CustomerService.DTOs.DependentDTOs;
using MediatR;

namespace CustomerService.Queries.DependentQueries;

public record class GetAllDependentQueries : IRequest<ApiResponse<List<ReadDependentDTO>>>;