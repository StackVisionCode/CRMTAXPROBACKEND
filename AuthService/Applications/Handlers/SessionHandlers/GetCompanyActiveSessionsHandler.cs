using AuthService.DTOs.SessionDTOs;
using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.SessionQueries;

namespace Handlers.SessionHandlers;

public class GetCompanyActiveSessionsHandler
    : IRequestHandler<GetCompanyActiveSessionsQuery, ApiResponse<List<SessionDTO>>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCompanyActiveSessionsHandler> _logger;

    public GetCompanyActiveSessionsHandler(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<GetCompanyActiveSessionsHandler> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<List<SessionDTO>>> Handle(
        GetCompanyActiveSessionsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // 1. Obtener la empresa del usuario solicitante
            var requestingUserCompanyQuery =
                from u in _context.TaxUsers
                where u.Id == request.RequestingUserId && u.IsActive
                select u.CompanyId;

            var requestingUserCompanyId = await requestingUserCompanyQuery.FirstOrDefaultAsync(
                cancellationToken
            );

            if (requestingUserCompanyId == Guid.Empty)
            {
                _logger.LogWarning(
                    "Requesting user not found or inactive: {UserId}",
                    request.RequestingUserId
                );
                return new ApiResponse<List<SessionDTO>>(
                    false,
                    "User not found or inactive",
                    new List<SessionDTO>()
                );
            }

            // 2. Obtener sesiones activas de todos los usuarios de la misma empresa
            var activeSessionsQuery =
                from s in _context.Sessions
                join u in _context.TaxUsers on s.TaxUserId equals u.Id
                where
                    u.CompanyId == requestingUserCompanyId
                    && u.IsActive
                    && !s.IsRevoke
                    && s.ExpireTokenRequest > DateTime.UtcNow
                orderby s.CreatedAt descending
                select new SessionDTO
                {
                    Id = s.Id,
                    TaxUserId = s.TaxUserId,
                    TokenRequest = "***HIDDEN***", // Por seguridad, no mostrar tokens
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

            var activeSessions = await activeSessionsQuery.ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} active company sessions for user {UserId} company {CompanyId}",
                activeSessions.Count,
                request.RequestingUserId,
                requestingUserCompanyId
            );

            return new ApiResponse<List<SessionDTO>>(
                true,
                "Company active sessions retrieved successfully",
                activeSessions
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving company active sessions for user {UserId}",
                request.RequestingUserId
            );
            return new ApiResponse<List<SessionDTO>>(
                false,
                "An error occurred while retrieving company sessions",
                new List<SessionDTO>()
            );
        }
    }
}
