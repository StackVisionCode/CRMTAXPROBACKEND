using AutoMapper;
using Common;
using CustomerService.Commands.TaxInformationCommands;
using CustomerService.Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.TaxInformationHandlers;

public class CreateTaxInformationHandler : IRequestHandler<CreateTaxInformationCommands, ApiResponse<bool>>
{
  private readonly ILogger<CreateTaxInformationHandler> _logger;
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  public CreateTaxInformationHandler(ILogger<CreateTaxInformationHandler> logger, ApplicationDbContext dbContext, IMapper mapper)
  {
    _logger = logger;
    _dbContext = dbContext;
    _mapper = mapper;
  }
  public async Task<ApiResponse<bool>> Handle(CreateTaxInformationCommands request, CancellationToken cancellationToken)
  {
    try
    {
      var exists = await _dbContext.TaxInformations.AnyAsync(c => c.CustomerId == request.taxInformation.CustomerId && c.FilingStatusId == request.taxInformation.FilingStatusId && c.BankAccountNumber == request.taxInformation.BankAccountNumber, cancellationToken);

      if (exists)
      {
        _logger.LogWarning("TaxInformation already exists with BankAccountNumber: {BankAccountNumber}", request.taxInformation.BankAccountNumber);
        return new ApiResponse<bool>(false, "TaxInformation with this BankAccountNumber already exists.", false);
      }
      var taxInformation = _mapper.Map<Domains.Customers.TaxInformation>(request.taxInformation);
      taxInformation.CreatedAt = DateTime.UtcNow;
      await _dbContext.TaxInformations.AddAsync(taxInformation, cancellationToken);
      var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
      _logger.LogInformation("TaxInformation created successfully: {TaxInformation}", taxInformation);
      return new ApiResponse<bool>(result, result ? "TaxInformation created successfully" : "Failed to create TaxInformation", result);
    }
    catch (Exception ex)
    {
      _logger.LogError("Error creating TaxInformation: {Message}", ex.Message);
      return new ApiResponse<bool>(false, ex.Message, false);
    }
  }
}