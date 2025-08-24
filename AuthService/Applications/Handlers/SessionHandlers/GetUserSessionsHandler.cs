using AuthService.DTOs.SessionDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.SessionQueries;

namespace Handlers.SessionHandlers;

public class GetUserSessionsHandler
    : IRequestHandler<GetUserSessionsQuery, ApiResponse<List<SessionWithUserDTO>>>
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

    public async Task<ApiResponse<List<SessionWithUserDTO>>> Handle(
        GetUserSessionsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // 1. Verificar que ambos usuarios pertenecen a la misma empresa
            var usersCompanyQuery =
                from u in _context.TaxUsers
                where
                    (u.Id == request.RequestingUserId || u.Id == request.TargetUserId) && u.IsActive
                select new { u.Id, u.CompanyId };

            var usersCompany = await usersCompanyQuery.ToListAsync(cancellationToken);

            if (usersCompany.Count != 2)
            {
                _logger.LogWarning(
                    "One or both users not found: Requesting={RequestingUserId}, Target={TargetUserId}",
                    request.RequestingUserId,
                    request.TargetUserId
                );
                return new ApiResponse<List<SessionWithUserDTO>>(
                    false,
                    "One or both users not found or inactive",
                    new List<SessionWithUserDTO>()
                );
            }

            var requestingUserCompany = usersCompany
                .First(x => x.Id == request.RequestingUserId)
                .CompanyId;
            var targetUserCompany = usersCompany.First(x => x.Id == request.TargetUserId).CompanyId;

            if (requestingUserCompany != targetUserCompany)
            {
                _logger.LogWarning(
                    "Users belong to different companies: Requesting={RequestingCompany}, Target={TargetCompany}",
                    requestingUserCompany,
                    targetUserCompany
                );
                return new ApiResponse<List<SessionWithUserDTO>>(
                    false,
                    "Access denied: Users belong to different companies",
                    new List<SessionWithUserDTO>()
                );
            }

            // 2. Obtener sesiones del usuario objetivo (últimos 30 días)
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            var userSessionsQuery =
                from s in _context.Sessions
                join u in _context.TaxUsers on s.TaxUserId equals u.Id
                where
                    s.TaxUserId == request.TargetUserId
                    && u.IsActive
                    && s.CreatedAt >= thirtyDaysAgo
                orderby s.CreatedAt descending
                select new
                {
                    s.Id,
                    s.TaxUserId,
                    s.ExpireTokenRequest,
                    s.TokenRefresh,
                    s.IpAddress,
                    s.Latitude,
                    s.Longitude,
                    s.Device,
                    s.IsRevoke,
                    s.CreatedAt,
                    u.Email,
                    u.Name,
                    u.LastName,
                    u.PhotoUrl,
                    u.IsActive,
                    u.IsOwner,
                    s.City,
                    s.Country,
                    s.Region,
                };

            var userSessionsRaw = await userSessionsQuery.ToListAsync(cancellationToken);

            var userSessions = userSessionsRaw
                .Select(s => new SessionWithUserDTO
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
                    CreatedAt = s.CreatedAt,

                    // Información del usuario
                    UserEmail = s.Email,
                    UserName = s.Name,
                    UserLastName = s.LastName,
                    UserPhotoUrl = s.PhotoUrl,
                    UserIsActive = s.IsActive,
                    UserIsOwner = s.IsOwner,

                    // Información de geolocalización
                    Latitude =
                        s.Latitude != null && double.TryParse(s.Latitude, out var lat)
                            ? lat
                            : (double?)null,
                    Longitude =
                        s.Longitude != null && double.TryParse(s.Longitude, out var lng)
                            ? lng
                            : (double?)null,

                    // Campos adicionales
                    City = s.City,
                    Country = s.Country,
                    Region = s.Region,
                })
                .ToList();

            _logger.LogInformation(
                "Retrieved {Count} sessions for target user {TargetUserId} requested by {RequestingUserId}",
                userSessions.Count,
                request.TargetUserId,
                request.RequestingUserId
            );

            return new ApiResponse<List<SessionWithUserDTO>>(
                true,
                "User sessions retrieved successfully",
                userSessions
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving sessions for target user {TargetUserId} requested by {RequestingUserId}",
                request.TargetUserId,
                request.RequestingUserId
            );
            return new ApiResponse<List<SessionWithUserDTO>>(
                false,
                "An error occurred while retrieving user sessions",
                new List<SessionWithUserDTO>()
            );
        }
    }
}
