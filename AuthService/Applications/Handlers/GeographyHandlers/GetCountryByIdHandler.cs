using AutoMapper;
using Common;
using DTOs.GeographyDTOs;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.GeographyQueries;

namespace Handlers.GeographyHandlers;

public class GetCountryByIdHandler : IRequestHandler<GetCountryByIdQuery, ApiResponse<CountryDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCountryByIdHandler> _logger;

    public GetCountryByIdHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<GetCountryByIdHandler> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<CountryDTO>> Handle(
        GetCountryByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // JOIN Countries ↔ States
            var country = await (
                from c in _dbContext.Countries
                where c.Id == request.CountryId && c.DeleteAt == null
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
            ).FirstOrDefaultAsync(cancellationToken);

            if (country is null)
            {
                return new ApiResponse<CountryDTO>(false, "Country not found");
            }

            _logger.LogInformation(
                "Country retrieved successfully: {CountryId}",
                request.CountryId
            );
            return new ApiResponse<CountryDTO>(true, "Country retrieved successfully", country);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error fetching country {Id}: {Message}",
                request.CountryId,
                ex.Message
            );
            return new ApiResponse<CountryDTO>(false, ex.Message);
        }
    }
}
