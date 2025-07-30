using AuthService.DTOs.SessionDTOs;
using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.SessionQueries;

namespace Handlers.SessionHandlers;

public class GetSessionByIdHandler : IRequestHandler<GetSessionByIdQuery, ApiResponse<SessionDTO>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetSessionByIdHandler> _logger;

    public GetSessionByIdHandler(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<GetSessionByIdHandler> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<SessionDTO>> Handle(
        GetSessionByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var sessionQuery = from s in _context.Sessions where s.Id == request.SessionId select s;

            var session = await sessionQuery.FirstOrDefaultAsync(cancellationToken);

            if (session == null)
            {
                _logger.LogWarning("Session {SessionId} not found", request.SessionId);
                return new ApiResponse<SessionDTO>(false, "Session not found", null!);
            }

            var sessionDTO = _mapper.Map<SessionDTO>(session);

            _logger.LogInformation("Retrieved session {SessionId}", request.SessionId);
            return new ApiResponse<SessionDTO>(true, "Session retrieved", sessionDTO);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session {SessionId}", request.SessionId);
            return new ApiResponse<SessionDTO>(
                false,
                "An error occurred while retrieving the session",
                null!
            );
        }
    }
}
