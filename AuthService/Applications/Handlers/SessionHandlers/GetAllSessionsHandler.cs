using AuthService.DTOs.PaginationDTO;
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
            var sessions = await _context
                .Sessions.Include(s => s.TaxUser)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var sessionDtos = _mapper.Map<List<SessionDTO>>(sessions);

            _logger.LogInformation("Sessions retrieved successfully: {Sessions}", sessionDtos);
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
                "An error occurred while retrieving sessions"
            );
        }
    }
}
