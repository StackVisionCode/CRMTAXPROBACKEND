using AutoMapper;
using Common;
using CustomerService.Commands.MaritalStatusCommands;
using CustomerService.Infrastructure.Context;
using MediatR;

namespace CustomerService.Handlers.MaritalStatusHandlers;

public class CreateMaritalStatusHandler
    : IRequestHandler<CreateMaritalStatusCommands, ApiResponse<bool>>
{
    private readonly ILogger<CreateMaritalStatusHandler> _logger;
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _dbContext;

    public CreateMaritalStatusHandler(
        ILogger<CreateMaritalStatusHandler> logger,
        IMapper mapper,
        ApplicationDbContext dbContext
    )
    {
        _logger = logger;
        _mapper = mapper;
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<bool>> Handle(
        CreateMaritalStatusCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var maritalStatus = _mapper.Map<Domains.Customers.MaritalStatus>(request.maritalStatus);
            maritalStatus.CreatedAt = DateTime.UtcNow;
            await _dbContext.MaritalStatuses.AddAsync(maritalStatus, cancellationToken);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            _logger.LogInformation(
                "MaritalStatus created successfully: {MaritalStatus}",
                maritalStatus
            );
            return new ApiResponse<bool>(
                result,
                result ? "MaritalStatus created successfully" : "Failed to create MaritalStatus",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating MaritalStatus: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
