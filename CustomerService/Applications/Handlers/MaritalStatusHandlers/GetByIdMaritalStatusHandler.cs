using AutoMapper;
using Common;
using CustomerService.DTOs.MaritalStatusDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.MaritalStatusDto;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.MaritalStatusHandlers;

public class GetByIdMaritalStatusHandler
    : IRequestHandler<GetByIdMaritalStatusQueries, ApiResponse<ReadMaritalStatusDto>>
{
    private readonly ILogger<GetByIdMaritalStatusHandler> _logger;
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _dbContext;

    public GetByIdMaritalStatusHandler(
        ILogger<GetByIdMaritalStatusHandler> logger,
        IMapper mapper,
        ApplicationDbContext dbContext
    )
    {
        _logger = logger;
        _mapper = mapper;
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<ReadMaritalStatusDto>> Handle(
        GetByIdMaritalStatusQueries request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var maritalStatus = await _dbContext.MaritalStatuses.FirstOrDefaultAsync(c =>
                c.Id == request.Id
            );
            if (maritalStatus == null)
            {
                _logger.LogWarning("MaritalStatus with ID {Id} not found.", request.Id);
                return new ApiResponse<ReadMaritalStatusDto>(
                    false,
                    "MaritalStatus not found",
                    null!
                );
            }
            var MaritalStatusDTO = _mapper.Map<ReadMaritalStatusDto>(maritalStatus);
            return new ApiResponse<ReadMaritalStatusDto>(
                true,
                "MaritalStatus retrieved successfully",
                MaritalStatusDTO
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving MaritalStatus: {Message}", ex.Message);
            return new ApiResponse<ReadMaritalStatusDto>(false, ex.Message, null!);
        }
    }
}
