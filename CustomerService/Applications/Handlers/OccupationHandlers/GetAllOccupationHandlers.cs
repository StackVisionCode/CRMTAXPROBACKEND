using AutoMapper;
using Common;
using CustomerService.DTOs.OccupationDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.OccupationQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.OccupationHandlers;

public class GetAllOccupationHandlers
    : IRequestHandler<GetAllOccupationQueries, ApiResponse<List<ReadOccupationDTO>>>
{
    private readonly ILogger<GetAllOccupationHandlers> _logger;
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _dbContext;

    public GetAllOccupationHandlers(
        ILogger<GetAllOccupationHandlers> logger,
        IMapper mapper,
        ApplicationDbContext dbContext
    )
    {
        _logger = logger;
        _mapper = mapper;
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<List<ReadOccupationDTO>>> Handle(
        GetAllOccupationQueries request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await _dbContext.Occupations.ToListAsync(cancellationToken);
            if (result is null || !result.Any())
            {
                _logger.LogInformation("No Occupations found");
                return new ApiResponse<List<ReadOccupationDTO>>(
                    false,
                    "No Occupations found",
                    null!
                );
            }
            var OccupationDTOs = _mapper.Map<List<ReadOccupationDTO>>(result);
            _logger.LogInformation(
                "Occupations retrieved successfully. {Occupations}",
                OccupationDTOs
            );
            return new ApiResponse<List<ReadOccupationDTO>>(
                true,
                "Occupations retrieved successfully.",
                OccupationDTOs
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving Occupations: {Message}", ex.Message);
            return new ApiResponse<List<ReadOccupationDTO>>(false, ex.Message, null!);
        }
    }
}
