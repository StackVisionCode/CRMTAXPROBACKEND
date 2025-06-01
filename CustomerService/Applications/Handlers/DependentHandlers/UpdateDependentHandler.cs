using AutoMapper;
using Common;
using CustomerService.Commands.DependentCommands;
using CustomerService.Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.DependentHandlers;

public class UpdateDependentHandler : IRequestHandler<UpdateDependentCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateDependentHandler> _logger;

    public UpdateDependentHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<UpdateDependentHandler> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        UpdateDependentCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var existingDependent = await _dbContext.Dependents.FirstOrDefaultAsync(
                d => d.Id == request.dependent.Id,
                cancellationToken
            );

            if (existingDependent == null)
            {
                _logger.LogWarning(
                    "Dependent with ID {Id} not found for update",
                    request.dependent.Id
                );
                return new ApiResponse<bool>(false, "Dependent not found", false);
            }

            var duplicateExists = await _dbContext.Dependents.AnyAsync(
                d =>
                    d.CustomerId == request.dependent.CustomerId
                    && d.FullName!.Trim() == request.dependent.FullName!.Trim()
                    && d.DateOfBirth == request.dependent.DateOfBirth
                    && d.RelationshipId == request.dependent.RelationshipId
                    && d.Id != request.dependent.Id,
                cancellationToken
            );

            if (duplicateExists)
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

            // Map the updated properties
            _mapper.Map(request.dependent, existingDependent);
            existingDependent.UpdatedAt = DateTime.UtcNow;

            _dbContext.Dependents.Update(existingDependent);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (result)
            {
                _logger.LogInformation(
                    "Dependent updated successfully: {Dependent}",
                    existingDependent
                );
                return new ApiResponse<bool>(true, "Dependent updated successfully", true);
            }
            else
            {
                _logger.LogWarning("Failed to update Dependent: {Dependent}", existingDependent);
                return new ApiResponse<bool>(false, "Failed to update Dependent", false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dependent: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
