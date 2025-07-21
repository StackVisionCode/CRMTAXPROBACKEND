using AuthService.DTOs.CompanyUserSessionDTOs;
using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyUserQueries;

namespace Handlers.CompanyUserHandlers;

public class GetActiveCompanyUserSessionsHandler
    : IRequestHandler<GetActiveCompanyUserSessionsQuery, ApiResponse<List<CompanyUserSessionDTO>>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetActiveCompanyUserSessionsHandler> _logger;

    public GetActiveCompanyUserSessionsHandler(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<GetActiveCompanyUserSessionsHandler> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<List<CompanyUserSessionDTO>>> Handle(
        GetActiveCompanyUserSessionsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var activeSessions = await _context
                .CompanyUserSessions.Where(s =>
                    s.CompanyUserId == request.CompanyUserId
                    && !s.IsRevoke
                    && s.ExpireTokenRequest > DateTime.UtcNow
                )
                .ToListAsync(cancellationToken);

            var sessionDTOs = _mapper.Map<List<CompanyUserSessionDTO>>(activeSessions);

            _logger.LogInformation(
                "Retrieved {Count} active sessions for company user {CompanyUserId}",
                sessionDTOs.Count,
                request.CompanyUserId
            );

            return new ApiResponse<List<CompanyUserSessionDTO>>(
                true,
                "Active sessions retrieved",
                sessionDTOs
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving active sessions for company user {CompanyUserId}",
                request.CompanyUserId
            );
            return new ApiResponse<List<CompanyUserSessionDTO>>(
                false,
                "An error occurred while retrieving active sessions"
            );
        }
    }
}
