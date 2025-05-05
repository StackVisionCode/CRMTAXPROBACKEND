using AuthService.DTOs.SessionDTOs;
using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.SessionQueries;

namespace Handlers.SessionHandlers;

public class GetActiveSessionsHandler : IRequestHandler<GetActiveSessionsQuery, ApiResponse<List<SessionDTO>>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetActiveSessionsHandler> _logger;

    public GetActiveSessionsHandler(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<GetActiveSessionsHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<List<SessionDTO>>> Handle(GetActiveSessionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var activeSessions = await _context.Sessions
                .Where(s => s.TaxUserId == request.UserId && !s.IsRevoke && s.ExpireTokenRequest > DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            var sessionDTOs = _mapper.Map<List<SessionDTO>>(activeSessions);

            _logger.LogInformation("Retrieved {Count} active sessions for user {UserId}", sessionDTOs.Count, request.UserId);
            return new ApiResponse<List<SessionDTO>>(true, "Active sessions retrieved", sessionDTOs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active sessions for user {UserId}", request.UserId);
            return new ApiResponse<List<SessionDTO>>(false, "An error occurred while retrieving active sessions");
        }
    }
}