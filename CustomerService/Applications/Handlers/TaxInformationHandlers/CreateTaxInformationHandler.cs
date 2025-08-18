using AutoMapper;
using Common;
using CustomerService.Commands.TaxInformationCommands;
using CustomerService.Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.TaxInformationHandlers;

public class CreateTaxInformationHandler
    : IRequestHandler<CreateTaxInformationCommands, ApiResponse<bool>>
{
    private readonly ILogger<CreateTaxInformationHandler> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public CreateTaxInformationHandler(
        ILogger<CreateTaxInformationHandler> logger,
        ApplicationDbContext dbContext,
        IMapper mapper
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ApiResponse<bool>> Handle(
        CreateTaxInformationCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Verificar duplicado mejorado
            var exists = await _dbContext.TaxInformations.AnyAsync(
                c => c.CustomerId == request.taxInformation.CustomerId,
                cancellationToken
            );

            if (exists)
            {
                _logger.LogWarning(
                    "Tax Information already exists for Customer: {CustomerId}",
                    request.taxInformation.CustomerId
                );
                return new ApiResponse<bool>(
                    false,
                    "Tax information already exists for this customer.",
                    false
                );
            }

            // Verificar cuenta bancaria duplicada si se proporciona
            if (!string.IsNullOrEmpty(request.taxInformation.BankAccountNumber))
            {
                var bankExists = await _dbContext.TaxInformations.AnyAsync(
                    c => c.BankAccountNumber == request.taxInformation.BankAccountNumber,
                    cancellationToken
                );

                if (bankExists)
                {
                    _logger.LogWarning(
                        "Bank Account Number already exists: {BankAccountNumber}",
                        request.taxInformation.BankAccountNumber
                    );
                    return new ApiResponse<bool>(
                        false,
                        "This bank account number is already registered.",
                        false
                    );
                }
            }

            var taxInformation = _mapper.Map<Domains.Customers.TaxInformation>(
                request.taxInformation
            );
            taxInformation.CreatedAt = DateTime.UtcNow;

            await _dbContext.TaxInformations.AddAsync(taxInformation, cancellationToken);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                _logger.LogInformation(
                    "TaxInformation created successfully: {TaxInformationId} for Customer: {CustomerId} by TaxUser: {CreatedBy}",
                    taxInformation.Id,
                    taxInformation.CustomerId,
                    taxInformation.CreatedByTaxUserId
                );
            }

            return new ApiResponse<bool>(
                result,
                result ? "TaxInformation created successfully" : "Failed to create TaxInformation",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating TaxInformation: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
