using AutoMapper;
using Common;
using CustomerService.DTOs.OccupationDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.OccupationQueries;
using MediatR;

namespace CustomerService.Handlers.OccupationHandlers;

public class GetByIdOccupationHandlers
    : IRequestHandler<GetByIdOccupationQueries, ApiResponse<ReadOccupationDTO>>
{
    private readonly ILogger<GetByIdOccupationHandlers> _logger;
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _dbContext;

    public GetByIdOccupationHandlers(
        ILogger<GetByIdOccupationHandlers> logger,
        IMapper mapper,
        ApplicationDbContext dbContext
    )
    {
        _logger = logger;
        _mapper = mapper;
        _dbContext = dbContext;
    }

    public Task<ApiResponse<ReadOccupationDTO>> Handle(
        GetByIdOccupationQueries request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = _dbContext.Occupations.FirstOrDefault(c => c.Id == request.Id);
            if (result is null)
            {
                _logger.LogInformation("No Occupation found with id: {Id}", request.Id);
                return Task.FromResult(
                    new ApiResponse<ReadOccupationDTO>(false, "No Occupation found", null!)
                );
            }
            var OccupationDTO = _mapper.Map<ReadOccupationDTO>(result);
            _logger.LogInformation(
                "Occupation retrieved successfully: {Occupation}",
                OccupationDTO
            );
            return Task.FromResult(
                new ApiResponse<ReadOccupationDTO>(
                    true,
                    "Occupation retrieved successfully",
                    OccupationDTO
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting Occupation: {Message}", ex.Message);
            return Task.FromResult(new ApiResponse<ReadOccupationDTO>(false, ex.Message, null!));
        }
    }
}
