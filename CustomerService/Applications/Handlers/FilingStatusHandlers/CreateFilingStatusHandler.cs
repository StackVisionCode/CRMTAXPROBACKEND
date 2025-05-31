using AutoMapper;
using Common;
using CustomerService.Commands.FilingStatusCommands;
using CustomerService.Infrastructure.Context;
using MediatR;

namespace CustomerService.Handlers.FilingStatusHandlers;

public class CreateFilingStatusHandler
    : IRequestHandler<CreateFilingStatusCommands, ApiResponse<bool>>
{
    private readonly ILogger<CreateFilingStatusHandler> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public CreateFilingStatusHandler(
        ILogger<CreateFilingStatusHandler> logger,
        ApplicationDbContext dbContext,
        IMapper mapper
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ApiResponse<bool>> Handle(
        CreateFilingStatusCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var filingStatus = _mapper.Map<Domains.Customers.FilingStatus>(request.filingStatus);
            filingStatus.CreatedAt = DateTime.UtcNow;
            await _dbContext.FilingStatuses.AddAsync(filingStatus, cancellationToken);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            _logger.LogInformation(
                "FilingStatus created successfully: {FilingStatus}",
                filingStatus
            );
            return new ApiResponse<bool>(
                result,
                result ? "FilingStatus created successfully" : "Failed to create FilingStatus",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating FilingStatus: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
