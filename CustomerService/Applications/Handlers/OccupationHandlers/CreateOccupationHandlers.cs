using AutoMapper;
using Common;
using CustomerService.Coommands.OccupationCommands;
using CustomerService.Infrastructure.Context;
using MediatR;

namespace CustomerService.Handlers.OccupationHandlers;

public class CreateOccupationHandlers : IRequestHandler<CreateOccupationCommands, ApiResponse<bool>>
{
    private readonly ILogger<CreateOccupationHandlers> _logger;
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _dbContext;

    public CreateOccupationHandlers(
        ILogger<CreateOccupationHandlers> logger,
        IMapper mapper,
        ApplicationDbContext dbContext
    )
    {
        _logger = logger;
        _mapper = mapper;
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<bool>> Handle(
        CreateOccupationCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var occupation = _mapper.Map<Domains.Customers.Occupation>(request.occupation);
            occupation.CreatedAt = DateTime.UtcNow;
            await _dbContext.Occupations.AddAsync(occupation, cancellationToken);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            _logger.LogInformation("Occupation created successfully: {Occupation}", occupation);
            return new ApiResponse<bool>(
                result,
                result ? "Occupation created successfully" : "Failed to create Occupation",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating Occupation: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
