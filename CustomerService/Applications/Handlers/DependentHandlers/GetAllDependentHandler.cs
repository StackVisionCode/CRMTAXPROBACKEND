using AutoMapper;
using Common;
using CustomerService.DTOs.DependentDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.DependentQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.DependentHandlers;

public class GetAllDependentHandler
    : IRequestHandler<GetAllDependentQueries, ApiResponse<List<ReadDependentDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllDependentHandler> _logger;

    public GetAllDependentHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<GetAllDependentHandler> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<List<ReadDependentDTO>>> Handle(
        GetAllDependentQueries request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await (
                from dependent in _dbContext.Dependents
                join customer in _dbContext.Customers on dependent.CustomerId equals customer.Id
                join relationship in _dbContext.Relationships
                    on dependent.RelationshipId equals relationship.Id
                select new ReadDependentDTO
                {
                    Id = dependent.Id,
                    CustomerId = dependent.CustomerId,
                    RelationshipId = dependent.RelationshipId,
                    FullName = dependent.FullName,
                    DateOfBirth = dependent.DateOfBirth,
                    Customer = customer.FirstName + " " + customer.LastName,
                    Relationship = relationship.Name,
                    // NUEVOS: Auditoría
                    CreatedAt = dependent.CreatedAt,
                    CreatedByTaxUserId = dependent.CreatedByTaxUserId,
                    UpdatedAt = dependent.UpdatedAt,
                    LastModifiedByTaxUserId = dependent.LastModifiedByTaxUserId,
                }
            ).ToListAsync(cancellationToken);

            if (result is null || !result.Any())
            {
                _logger.LogInformation("No dependents found.");
                return new ApiResponse<List<ReadDependentDTO>>(
                    false,
                    "No dependents found.",
                    new List<ReadDependentDTO>()
                );
            }

            _logger.LogInformation(
                "Dependents retrieved successfully: {Count} records",
                result.Count
            );
            return new ApiResponse<List<ReadDependentDTO>>(
                true,
                "Dependents retrieved successfully.",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting dependents.");
            return new ApiResponse<List<ReadDependentDTO>>(
                false,
                ex.Message,
                new List<ReadDependentDTO>()
            );
        }
    }
}
