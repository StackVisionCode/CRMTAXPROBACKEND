using AutoMapper;
using Common;
using DTOs.GeographyDTOs;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.GeographyQueries;

namespace Handlers.GeographyHandlers;

public class GetStateByIdHandler : IRequestHandler<GetStateByIdQuery, ApiResponse<StateDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetStateByIdHandler> _logger;

    public GetStateByIdHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<GetStateByIdHandler> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<StateDTO>> Handle(
        GetStateByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // JOIN States ↔ Countries
            var state = await (
                from s in _dbContext.States
                join c in _dbContext.Countries on s.CountryId equals c.Id
                where s.Id == request.StateId && s.DeleteAt == null && c.DeleteAt == null
                select new StateDTO
                {
                    Id = s.Id,
                    Name = s.Name,
                    CountryId = s.CountryId,
                    CountryName = c.Name,
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (state is null)
            {
                return new ApiResponse<StateDTO>(false, "State not found");
            }

            _logger.LogInformation("State retrieved successfully: {StateId}", request.StateId);
            return new ApiResponse<StateDTO>(true, "State retrieved successfully", state);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error fetching state {Id}: {Message}",
                request.StateId,
                ex.Message
            );
            return new ApiResponse<StateDTO>(false, ex.Message);
        }
    }
}
