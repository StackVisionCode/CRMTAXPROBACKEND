using Common;
using CustomerService.DTOs.DependentDTOs;
using MediatR;

namespace CustomerService.Queries.DependentQueries;

public record class GetByIdDependentQueries(Guid Id) : IRequest<ApiResponse<ReadDependentDTO>>;