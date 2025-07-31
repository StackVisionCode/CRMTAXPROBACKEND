using AutoMapper;
using Common;
using DTOs.GeographyDTOs;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.GeographyQueries;

namespace Handlers.GeographyHandlers;

public class GetAllStatesHandler : IRequestHandler<GetAllStatesQuery, ApiResponse<List<StateDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllStatesHandler> _logger;

    public GetAllStatesHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<GetAllStatesHandler> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<List<StateDTO>>> Handle(
        GetAllStatesQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // JOIN States ↔ Countries
            var states = await (
                from s in _dbContext.States
                join c in _dbContext.Countries on s.CountryId equals c.Id
                where s.DeleteAt == null && c.DeleteAt == null
                select new StateDTO
                {
                    Id = s.Id,
                    Name = s.Name,
                    CountryId = s.CountryId,
                    CountryName = c.Name,
                }
            ).OrderBy(s => s.CountryName).ThenBy(s => s.Name).ToListAsync(cancellationToken);

            if (!states.Any())
            {
                return new ApiResponse<List<StateDTO>>(
                    false,
                    "No states found",
                    new List<StateDTO>()
                );
            }

            _logger.LogInformation("States retrieved successfully: {Count}", states.Count);
            return new ApiResponse<List<StateDTO>>(true, "States retrieved successfully", states);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving states: {Message}", ex.Message);
            return new ApiResponse<List<StateDTO>>(false, ex.Message, new List<StateDTO>());
        }
    }
}
