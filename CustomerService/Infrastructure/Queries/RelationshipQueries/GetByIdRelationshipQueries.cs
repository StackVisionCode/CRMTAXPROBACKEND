using Common;
using CustomerService.DTOs.RelationshipDTOs;
using MediatR;

namespace CustomerService.Queries.RelationshipQueries;

public record class GetByIdRelationshipQueries(Guid Id)
    : IRequest<ApiResponse<ReadRelationshipDTO>>;
