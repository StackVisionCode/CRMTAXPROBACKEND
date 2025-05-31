using AutoMapper;
using Common;
using CustomerService.DTOs.RelationshipDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.RelationshipQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.RelationshipHandlers;

public class GetAllRelationshipHandlers
    : IRequestHandler<GetAllRelationshipQueries, ApiResponse<List<ReadRelationshipDTO>>>
{
    private readonly ILogger<GetAllRelationshipHandlers> _logger;
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _dbContext;

    public GetAllRelationshipHandlers(
        ILogger<GetAllRelationshipHandlers> logger,
        IMapper mapper,
        ApplicationDbContext dbContext
    )
    {
        _logger = logger;
        _mapper = mapper;
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<List<ReadRelationshipDTO>>> Handle(
        GetAllRelationshipQueries request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await _dbContext.Relationships.ToListAsync(cancellationToken);
            if (result is null || !result.Any())
            {
                _logger.LogInformation("No Relationships found");
                return new ApiResponse<List<ReadRelationshipDTO>>(
                    false,
                    "No Relationships found",
                    null!
                );
            }
            var RelationshipDTOs = _mapper.Map<List<ReadRelationshipDTO>>(result);
            _logger.LogInformation(
                "Relationships retrieved successfully. {Relationships}",
                RelationshipDTOs
            );
            return new ApiResponse<List<ReadRelationshipDTO>>(
                true,
                "Relationships retrieved successfully.",
                RelationshipDTOs
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving Relationships: {Message}", ex.Message);
            return new ApiResponse<List<ReadRelationshipDTO>>(false, ex.Message, null!);
        }
    }
}
