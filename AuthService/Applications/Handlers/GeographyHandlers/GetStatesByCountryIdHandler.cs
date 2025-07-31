using AutoMapper;
using Common;
using DTOs.GeographyDTOs;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.GeographyQueries;

namespace Handlers.GeographyHandlers;

public class GetStatesByCountryIdHandler
    : IRequestHandler<GetStatesByCountryIdQuery, ApiResponse<List<StateDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetStatesByCountryIdHandler> _logger;

    public GetStatesByCountryIdHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<GetStatesByCountryIdHandler> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<List<StateDTO>>> Handle(
        GetStatesByCountryIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // JOIN States ↔ Countries
            var states = await (
                from s in _dbContext.States
                join c in _dbContext.Countries on s.CountryId equals c.Id
                where s.CountryId == request.CountryId && s.DeleteAt == null && c.DeleteAt == null
                select new StateDTO
                {
                    Id = s.Id,
                    Name = s.Name,
                    CountryId = s.CountryId,
                    CountryName = c.Name,
                }
            ).OrderBy(s => s.Name).ToListAsync(cancellationToken);

            if (!states.Any())
            {
                return new ApiResponse<List<StateDTO>>(
                    false,
                    $"No states found for country {request.CountryId}",
                    new List<StateDTO>()
                );
            }

            _logger.LogInformation(
                "States retrieved successfully for country {CountryId}: {Count}",
                request.CountryId,
                states.Count
            );
            return new ApiResponse<List<StateDTO>>(true, "States retrieved successfully", states);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving states for country {CountryId}: {Message}",
                request.CountryId,
                ex.Message
            );
            return new ApiResponse<List<StateDTO>>(false, ex.Message, new List<StateDTO>());
        }
    }
}
