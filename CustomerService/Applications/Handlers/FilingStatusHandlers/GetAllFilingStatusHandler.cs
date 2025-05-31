using AutoMapper;
using Common;
using CustomerService.DTOs.FilingStatusDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.FilingStatusQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.FilingStatusHandlers;

public class GetAllFilingStatusHandler
    : IRequestHandler<GetAllFilingStatusQueries, ApiResponse<List<ReadFilingStatusDto>>>
{
    private readonly ILogger<GetAllFilingStatusHandler> _logger;
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _dbContext;

    public GetAllFilingStatusHandler(
        ILogger<GetAllFilingStatusHandler> logger,
        IMapper mapper,
        ApplicationDbContext dbContext
    )
    {
        _logger = logger;
        _mapper = mapper;
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<List<ReadFilingStatusDto>>> Handle(
        GetAllFilingStatusQueries request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await _dbContext.FilingStatuses.ToListAsync(cancellationToken);

            if (result is null || !result.Any())
            {
                _logger.LogInformation("No FilingStatuses found");
                return new ApiResponse<List<ReadFilingStatusDto>>(
                    false,
                    "No FilingStatuses found.",
                    null!
                );
            }

            var FilingStatusDTOs = _mapper.Map<List<ReadFilingStatusDto>>(result);
            _logger.LogInformation(
                "FilingStatuses retrieved successfully. {FilingStatuses}",
                FilingStatusDTOs
            );
            return new ApiResponse<List<ReadFilingStatusDto>>(
                true,
                "FilingStatuses retrieved successfully.",
                FilingStatusDTOs
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving FilingStatuses: {Message}", ex.Message);
            return new ApiResponse<List<ReadFilingStatusDto>>(false, ex.Message, null!);
        }
    }
}
