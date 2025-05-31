using AutoMapper;
using Common;
using CustomerService.Coommands.RelationshipCommands;
using CustomerService.Infrastructure.Context;
using MediatR;

namespace CustomerService.Handlers.RelationshipHandlers;

public class CreateRelationshipHandlers
    : IRequestHandler<CreateRelationshipCommands, ApiResponse<bool>>
{
    private readonly ILogger<CreateRelationshipHandlers> _logger;
    private IMapper _mapper;
    private ApplicationDbContext _dbContext;

    public CreateRelationshipHandlers(
        ILogger<CreateRelationshipHandlers> logger,
        IMapper mapper,
        ApplicationDbContext dbContext
    )
    {
        _logger = logger;
        _mapper = mapper;
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<bool>> Handle(
        CreateRelationshipCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var relationship = _mapper.Map<Domains.Customers.Relationship>(request.RelationshipDTO);
            relationship.CreatedAt = DateTime.UtcNow;
            await _dbContext.Relationships.AddAsync(relationship, cancellationToken);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            _logger.LogInformation(
                "Relationship created successfully: {Relationship}",
                relationship
            );
            return new ApiResponse<bool>(
                result,
                result ? "Relationship created successfully" : "Failed to create Relationship",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating Relationship: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
