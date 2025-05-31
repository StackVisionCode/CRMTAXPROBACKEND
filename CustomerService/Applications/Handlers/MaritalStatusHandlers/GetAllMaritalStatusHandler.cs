using AutoMapper;
using Common;
using CustomerService.DTOs.MaritalStatusDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.MaritalStatusQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.MaritalStatusHandlers;

public class GetAllMaritalStatusHandler
    : IRequestHandler<GetAllMaritalStatusQueries, ApiResponse<List<ReadMaritalStatusDto>>>
{
    private readonly ILogger<GetAllMaritalStatusHandler> _logger;
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _dbContext;

    public GetAllMaritalStatusHandler(
        ILogger<GetAllMaritalStatusHandler> logger,
        IMapper mapper,
        ApplicationDbContext dbContext
    )
    {
        _logger = logger;
        _mapper = mapper;
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<List<ReadMaritalStatusDto>>> Handle(
        GetAllMaritalStatusQueries request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await _dbContext.MaritalStatuses.ToListAsync(cancellationToken);
            if (result is null || !result.Any())
            {
                _logger.LogInformation("No MaritalStatuses found");
                return new ApiResponse<List<ReadMaritalStatusDto>>(
                    false,
                    "No MaritalStatuses found",
                    null!
                );
            }
            var MaritalStatusDTOs = _mapper.Map<List<ReadMaritalStatusDto>>(result);
            _logger.LogInformation(
                "MaritalStatuses retrieved successfully. {MaritalStatuses}",
                MaritalStatusDTOs
            );
            return new ApiResponse<List<ReadMaritalStatusDto>>(
                true,
                "MaritalStatuses retrieved successfully.",
                MaritalStatusDTOs
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving MaritalStatuses: {Message}", ex.Message);
            return new ApiResponse<List<ReadMaritalStatusDto>>(false, ex.Message, null!);
        }
    }
}
