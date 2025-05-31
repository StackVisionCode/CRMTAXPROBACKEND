using AutoMapper;
using Common;
using CustomerService.DTOs.RelationshipDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.RelationshipQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.RelationshipHandlers;

public class GetByIdRelationshipHandlers
    : IRequestHandler<GetByIdRelationshipQueries, ApiResponse<ReadRelationshipDTO>>
{
    private readonly ILogger<GetByIdRelationshipHandlers> _logger;
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _dbContext;

    public GetByIdRelationshipHandlers(
        ILogger<GetByIdRelationshipHandlers> logger,
        IMapper mapper,
        ApplicationDbContext dbContext
    )
    {
        _logger = logger;
        _mapper = mapper;
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<ReadRelationshipDTO>> Handle(
        GetByIdRelationshipQueries request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await _dbContext.Relationships.FirstOrDefaultAsync(
                c => c.Id == request.Id,
                cancellationToken
            );
            if (result is null)
            {
                _logger.LogInformation("No Relationship found with id: {Id}", request.Id);
                return new ApiResponse<ReadRelationshipDTO>(false, "No Relationship found", null!);
            }
            var RelationshipDTO = _mapper.Map<ReadRelationshipDTO>(result);
            _logger.LogInformation(
                "Relationship retrieved successfully: {Relationship}",
                RelationshipDTO
            );
            return new ApiResponse<ReadRelationshipDTO>(
                true,
                "Relationship retrieved successfully",
                RelationshipDTO
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving Relationship: {Message}", ex.Message);
            return new ApiResponse<ReadRelationshipDTO>(false, ex.Message, null!);
        }
    }
}
