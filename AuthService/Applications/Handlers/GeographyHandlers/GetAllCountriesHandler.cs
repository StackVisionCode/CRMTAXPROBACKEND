using AutoMapper;
using Common;
using DTOs.GeographyDTOs;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.GeographyQueries;

namespace Handlers.GeographyHandlers;

public class GetAllCountriesHandler
    : IRequestHandler<GetAllCountriesQuery, ApiResponse<List<CountryDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllCountriesHandler> _logger;

    public GetAllCountriesHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<GetAllCountriesHandler> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<List<CountryDTO>>> Handle(
        GetAllCountriesQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // JOIN Countries ↔ States
            var countries = await (
                from c in _dbContext.Countries
                where c.DeleteAt == null
                join s in _dbContext.States on c.Id equals s.CountryId into states
                from s in states.DefaultIfEmpty()
                where s == null || s.DeleteAt == null

                group s by new { c.Id, c.Name } into g
                select new CountryDTO
                {
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    States = g.Where(s => s != null)
                        .Select(s => new StateDTO
                        {
                            Id = s!.Id,
                            Name = s.Name,
                            CountryId = s.CountryId,
                            CountryName = g.Key.Name,
                        })
                        .OrderBy(s => s.Name)
                        .ToList(),
                }
            ).OrderBy(c => c.Name).ToListAsync(cancellationToken);

            if (!countries.Any())
            {
                return new ApiResponse<List<CountryDTO>>(
                    false,
                    "No countries found",
                    new List<CountryDTO>()
                );
            }

            _logger.LogInformation("Countries retrieved successfully: {Count}", countries.Count);
            return new ApiResponse<List<CountryDTO>>(
                true,
                "Countries retrieved successfully",
                countries
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving countries: {Message}", ex.Message);
            return new ApiResponse<List<CountryDTO>>(false, ex.Message, new List<CountryDTO>());
        }
    }
}
