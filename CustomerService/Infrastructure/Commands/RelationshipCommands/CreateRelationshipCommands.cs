using Common;
using CustomerService.DTOs.RelationshipDTOs;
using MediatR;

namespace CustomerService.Coommands.RelationshipCommands;

public record class CreateRelationshipCommands(CreateRelationshipDTO RelationshipDTO)
    : IRequest<ApiResponse<bool>>;
