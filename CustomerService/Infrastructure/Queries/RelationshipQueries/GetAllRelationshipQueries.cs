using Common;
using CustomerService.DTOs.RelationshipDTOs;
using MediatR;

namespace CustomerService.Queries.RelationshipQueries;

public record class GetAllRelationshipQueries : IRequest<ApiResponse<List<ReadRelationshipDTO>>>;
