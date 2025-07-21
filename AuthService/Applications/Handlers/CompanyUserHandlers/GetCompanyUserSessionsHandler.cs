using AuthService.DTOs.CompanyUserSessionDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyUserQueries;

namespace Handlers.CompanyUserHandlers;

public class GetCompanyUserSessionsHandler
    : IRequestHandler<GetCompanyUserSessionsQuery, ApiResponse<List<ReadCompanyUserSessionDTO>>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<GetCompanyUserSessionsHandler> _logger;

    public GetCompanyUserSessionsHandler(
        ApplicationDbContext db,
        ILogger<GetCompanyUserSessionsHandler> logger
    )
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ApiResponse<List<ReadCompanyUserSessionDTO>>> Handle(
        GetCompanyUserSessionsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var sessions = await _db
                .CompanyUserSessions.Where(s => s.CompanyUserId == request.CompanyUserId)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new ReadCompanyUserSessionDTO
                {
                    SessionId = s.Id,
                    LoginAt = s.CreatedAt,
                    ExpireAt = s.ExpireTokenRequest,
                    Ip = s.IpAddress,
                    Device = s.Device,
                    IsRevoke = s.IsRevoke,
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} sessions for company user {CompanyUserId}",
                sessions.Count,
                request.CompanyUserId
            );
            return new ApiResponse<List<ReadCompanyUserSessionDTO>>(
                true,
                "Sessions retrieved successfully",
                sessions
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving sessions for company user {CompanyUserId}",
                request.CompanyUserId
            );
            return new ApiResponse<List<ReadCompanyUserSessionDTO>>(
                false,
                "Error retrieving sessions"
            );
        }
    }
}
