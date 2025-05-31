using CustomerService.DTOs.MaritalStatusDTOs;
using MediatR;

namespace CustomerService.Queries.MaritalStatusDto;

public record class GetByIdMaritalStatusQueries(Guid Id)
    : IRequest<Common.ApiResponse<ReadMaritalStatusDto>>;
