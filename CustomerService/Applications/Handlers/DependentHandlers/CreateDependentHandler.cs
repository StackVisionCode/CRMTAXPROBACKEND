using AutoMapper;
using Common;
using CustomerService.Commands.DependentCommands;
using CustomerService.Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.DependentHandlers;

public class CreateDependentHandler : IRequestHandler<CreateDependentCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateDependentHandler> _logger;

    public CreateDependentHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<CreateDependentHandler> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        CreateDependentCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var exists = await _dbContext.Dependents.AnyAsync(
                d =>
                    d.CustomerId == request.dependent.CustomerId
                    && d.FullName! == request.dependent.FullName!.Trim()
                    && d.DateOfBirth == request.dependent.DateOfBirth
                    && d.RelationshipId == request.dependent.RelationshipId,
                cancellationToken
            );

            if (exists)
            {
                _logger.LogWarning(
                    "Dependent already exists with CustomerId: {CustomerId}",
                    request.dependent.CustomerId
                );
                return new ApiResponse<bool>(
                    false,
                    "Dependent with this CustomerId already exists.",
                    false
                );
            }

            var dependent = _mapper.Map<Domains.Customers.Dependent>(request.dependent);
            dependent.CreatedAt = DateTime.UtcNow;
            await _dbContext.Dependents.AddAsync(dependent, cancellationToken);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            _logger.LogInformation("Dependent created successfully: {Dependent}", dependent);
            return new ApiResponse<bool>(
                result,
                result ? "Dependent created successfully" : "Failed to create Dependent",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating Dependent: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
