using AuthService.DTOs.SessionDTOs;
using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.SessionQueries;

namespace Handlers.SessionHandlers;

public class GetAllSessionsHandler
    : IRequestHandler<GetAllSessionsQuery, ApiResponse<List<SessionDTO>>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllSessionsHandler> _logger;

    public GetAllSessionsHandler(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<GetAllSessionsHandler> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<List<SessionDTO>>> Handle(
        GetAllSessionsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var sessionsQuery = from s in _context.Sessions select s;

            var sessions = await sessionsQuery.ToListAsync(cancellationToken);
            var sessionDtos = _mapper.Map<List<SessionDTO>>(sessions);

            _logger.LogInformation("Retrieved {Count} sessions successfully", sessionDtos.Count);
            return new ApiResponse<List<SessionDTO>>(
                true,
                "Sessions retrieved successfully",
                sessionDtos
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all sessions");
            return new ApiResponse<List<SessionDTO>>(
                false,
                "An error occurred while retrieving sessions",
                new List<SessionDTO>()
            );
        }
    }
}
