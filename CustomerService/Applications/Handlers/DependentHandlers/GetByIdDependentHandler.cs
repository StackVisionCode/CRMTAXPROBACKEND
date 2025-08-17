using AutoMapper;
using Common;
using CustomerService.DTOs.DependentDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.DependentQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.DependentHandlers;

public class GetByIdDependentHandler
    : IRequestHandler<GetByIdDependentQueries, ApiResponse<ReadDependentDTO>>
{
    private readonly ILogger<GetByIdDependentHandler> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetByIdDependentHandler(
        ILogger<GetByIdDependentHandler> logger,
        ApplicationDbContext dbContext,
        IMapper mapper
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ReadDependentDTO>> Handle(
        GetByIdDependentQueries request,
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
                where dependent.Id == request.Id
                select new ReadDependentDTO
                {
                    Id = dependent.Id,
                    CustomerId = dependent.CustomerId,
                    RelationshipId = dependent.RelationshipId,
                    FullName = dependent.FullName,
                    DateOfBirth = dependent.DateOfBirth,
                    Customer = customer.FirstName + " " + customer.LastName,
                    Relationship = relationship.Name,
                    // Auditoría
                    CreatedAt = dependent.CreatedAt,
                    CreatedByTaxUserId = dependent.CreatedByTaxUserId,
                    UpdatedAt = dependent.UpdatedAt,
                    LastModifiedByTaxUserId = dependent.LastModifiedByTaxUserId,
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (result is null)
            {
                _logger.LogInformation("No dependent found with ID {Id}.", request.Id);
                return new ApiResponse<ReadDependentDTO>(false, "No dependent found.", null!);
            }

            _logger.LogInformation("Dependent retrieved successfully: {DependentId}", result.Id);
            return new ApiResponse<ReadDependentDTO>(
                true,
                "Dependent retrieved successfully.",
                result
            );
        }
        catch (Exception e)
        {
            _logger.LogError("Error retrieving dependent: {Message}", e.Message);
            return new ApiResponse<ReadDependentDTO>(false, e.Message, null!);
        }
    }
}
