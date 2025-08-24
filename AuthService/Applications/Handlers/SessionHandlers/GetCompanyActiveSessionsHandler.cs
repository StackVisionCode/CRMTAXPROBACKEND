using AuthService.DTOs.SessionDTOs;
using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.SessionQueries;

namespace Handlers.SessionHandlers;

public class GetCompanyActiveSessionsHandler
    : IRequestHandler<GetCompanyActiveSessionsQuery, ApiResponse<List<SessionWithUserDTO>>>
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

    public async Task<ApiResponse<List<SessionWithUserDTO>>> Handle(
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
                return new ApiResponse<List<SessionWithUserDTO>>(
                    false,
                    "User not found or inactive",
                    new List<SessionWithUserDTO>()
                );
            }

            // 2. Obtener sesiones activas de todos los usuarios de la misma empresa
            var activeSessionsRawQuery =
                from s in _context.Sessions
                join u in _context.TaxUsers on s.TaxUserId equals u.Id
                where
                    u.CompanyId == requestingUserCompanyId
                    && u.IsActive
                    && !s.IsRevoke
                    && s.ExpireTokenRequest > DateTime.UtcNow
                orderby s.CreatedAt descending
                select new
                {
                    s.Id,
                    s.TaxUserId,
                    s.TokenRequest,
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

            var activeSessionsRaw = await activeSessionsRawQuery.ToListAsync(cancellationToken);

            var activeSessions = activeSessionsRaw
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

                    // Información de geolocalización (con manejo seguro de parsing)
                    Latitude =
                        s.Latitude != null && double.TryParse(s.Latitude, out var lat)
                            ? lat
                            : (double?)null,
                    Longitude =
                        s.Longitude != null && double.TryParse(s.Longitude, out var lng)
                            ? lng
                            : (double?)null,

                    // Campos adicionales para el frontend
                    City = s.City,
                    Country = s.Country,
                    Region = s.Region,
                })
                .ToList();

            _logger.LogInformation(
                "Retrieved {Count} active company sessions for user {UserId} company {CompanyId}",
                activeSessions.Count,
                request.RequestingUserId,
                requestingUserCompanyId
            );

            return new ApiResponse<List<SessionWithUserDTO>>(
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
            return new ApiResponse<List<SessionWithUserDTO>>( // ← CORREGIDO
                false,
                "An error occurred while retrieving company sessions",
                new List<SessionWithUserDTO>() // ← CORREGIDO
            );
        }
    }
}
