using AutoMapper;
using Common;
using CustomerService.DTOs.FilingStatusDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.FilingStatusQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.FilingStatusHandler;

public class GetByIdFilingStatusHandler
    : IRequestHandler<GetByIdFilingStatusQueries, ApiResponse<ReadFilingStatusDto>>
{
    private readonly ILogger<GetByIdFilingStatusHandler> _logger;
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _dbContext;

    public GetByIdFilingStatusHandler(
        ILogger<GetByIdFilingStatusHandler> logger,
        IMapper mapper,
        ApplicationDbContext dbContext
    )
    {
        _logger = logger;
        _mapper = mapper;
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<ReadFilingStatusDto>> Handle(
        GetByIdFilingStatusQueries request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await _dbContext.FilingStatuses.FirstOrDefaultAsync(
                c => c.Id == request.Id,
                cancellationToken
            );
            if (result == null)
            {
                _logger.LogWarning("FilingStatus with ID {Id} not found.", request.Id);
                return new ApiResponse<ReadFilingStatusDto>(false, "FilingStatus not found", null!);
            }
            var FilingStatusDTO = _mapper.Map<ReadFilingStatusDto>(result);
            return new ApiResponse<ReadFilingStatusDto>(
                true,
                "FilingStatus retrieved successfully",
                FilingStatusDTO
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting FilingStatus: {Message}", ex.Message);
            return new ApiResponse<ReadFilingStatusDto>(false, ex.Message, null!);
        }
    }
}
