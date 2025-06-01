using AutoMapper;
using Common;
using CustomerService.Commands.TaxInformationCommands;
using CustomerService.Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.TaxInformationHandlers;

public class UpdateTaxInformationHandler
    : IRequestHandler<UpdateTaxInformationCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateTaxInformationHandler> _logger;

    public UpdateTaxInformationHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<UpdateTaxInformationHandler> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        UpdateTaxInformationCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var existingTaxInformation = await _dbContext.TaxInformations.FirstOrDefaultAsync(
                c => c.Id == request.taxInformation.Id,
                cancellationToken
            );

            if (existingTaxInformation == null)
            {
                _logger.LogWarning(
                    "TaxInformation with ID {Id} not found for update",
                    request.taxInformation.Id
                );
                return new ApiResponse<bool>(false, "TaxInformation not found", false);
            }

            var duplicateExists = await _dbContext.TaxInformations.AnyAsync(
                c =>
                    c.CustomerId == request.taxInformation.CustomerId
                    && c.BankAccountNumber == request.taxInformation.BankAccountNumber
                    && c.Id != request.taxInformation.Id,
                cancellationToken
            );

            if (duplicateExists)
            {
                _logger.LogWarning(
                    "TaxInformation with TaxIdentificationNumber {BankAccountNumber} already exists",
                    request.taxInformation.BankAccountNumber
                );
                return new ApiResponse<bool>(
                    false,
                    "TaxInformation with this BankAccountNumber already exists",
                    false
                );
            }

            _mapper.Map(request.taxInformation, existingTaxInformation);
            existingTaxInformation.UpdatedAt = DateTime.UtcNow;

            _dbContext.TaxInformations.Update(existingTaxInformation);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                _logger.LogInformation(
                    "TaxInformation updated successfully: {TaxInformation}",
                    existingTaxInformation
                );
                return new ApiResponse<bool>(true, "TaxInformation updated successfully", true);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to update TaxInformation: {TaxInformationId}",
                    existingTaxInformation.Id
                );
                return new ApiResponse<bool>(false, "Failed to update TaxInformation", false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating TaxInformation: {TaxInformationId}",
                request.taxInformation.Id
            );
            return new ApiResponse<bool>(false, "Error updating TaxInformation", false);
        }
    }
}
