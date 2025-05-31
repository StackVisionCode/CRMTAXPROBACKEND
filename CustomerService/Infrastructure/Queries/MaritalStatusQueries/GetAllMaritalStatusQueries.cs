using Common;
using CustomerService.DTOs.MaritalStatusDTOs;
using MediatR;

namespace CustomerService.Queries.MaritalStatusQueries;

public record class GetAllMaritalStatusQueries : IRequest<ApiResponse<List<ReadMaritalStatusDto>>>;
