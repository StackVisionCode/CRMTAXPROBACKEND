using AuthService.DTOs.SessionDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.SessionQueries;

namespace Handlers.SessionHandlers;

public class GetUserSessionsHandler
    : IRequestHandler<GetUserSessionsQuery, ApiResponse<List<SessionDTO>>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetUserSessionsHandler> _logger;

    public GetUserSessionsHandler(
        ApplicationDbContext context,
        ILogger<GetUserSessionsHandler> logger
    )
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<List<SessionDTO>>> Handle(
        GetUserSessionsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // 1. Verificar que ambos usuarios pertenecen a la misma empresa
            var usersCompanyQuery =
                from u1 in _context.TaxUsers
                join u2 in _context.TaxUsers on u1.CompanyId equals u2.CompanyId
                where u1.Id == request.RequestingUserId && u2.Id == request.TargetUserId
                select new { RequestingUser = u1, TargetUser = u2 };

            var usersInfo = await usersCompanyQuery.FirstOrDefaultAsync(cancellationToken);

            if (usersInfo == null)
            {
                _logger.LogWarning(
                    "Users not found or don't belong to same company: Requesting={RequestingUserId}, Target={TargetUserId}",
                    request.RequestingUserId,
                    request.TargetUserId
                );
                return new ApiResponse<List<SessionDTO>>(
                    false,
                    "Users not found or don't belong to the same company",
                    new List<SessionDTO>()
                );
            }

            // 2. Obtener sesiones del usuario específico (últimas 30 días)
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            var userSessionsQuery =
                from s in _context.Sessions
                where s.TaxUserId == request.TargetUserId && s.CreatedAt >= thirtyDaysAgo
                orderby s.CreatedAt descending
                select new SessionDTO
                {
                    Id = s.Id,
                    TaxUserId = s.TaxUserId,
                    TokenRequest = "***HIDDEN***",
                    ExpireTokenRequest = s.ExpireTokenRequest,
                    TokenRefresh = s.TokenRefresh != null ? "***HIDDEN***" : null,
                    IpAddress = s.IpAddress,
                    Location =
                        s.Latitude != null && s.Longitude != null
                            ? $"{s.Latitude},{s.Longitude}"
                            : null,
                    Device = s.Device,
                    IsRevoke = s.IsRevoke,
                };

            var userSessions = await userSessionsQuery
                .Take(100) // Limitar a las últimas 100 sesiones
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} sessions for target user {TargetUserId} by requesting user {RequestingUserId}",
                userSessions.Count,
                request.TargetUserId,
                request.RequestingUserId
            );

            return new ApiResponse<List<SessionDTO>>(
                true,
                "User sessions retrieved successfully",
                userSessions
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving user sessions: Requesting={RequestingUserId}, Target={TargetUserId}",
                request.RequestingUserId,
                request.TargetUserId
            );
            return new ApiResponse<List<SessionDTO>>(
                false,
                "An error occurred while retrieving user sessions",
                new List<SessionDTO>()
            );
        }
    }
}
