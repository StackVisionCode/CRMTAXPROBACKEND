using AuthService.DTOs.SessionDTOs;
using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.SessionQueries;

namespace Handlers.SessionHandlers;

/// <summary>
/// Handler para obtener estadísticas de sesiones de la empresa
/// </summary>
public class GetCompanySessionStatsHandler
    : IRequestHandler<GetCompanySessionStatsQuery, ApiResponse<CompanySessionStatsDTO>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetCompanySessionStatsHandler> _logger;

    public GetCompanySessionStatsHandler(
        ApplicationDbContext context,
        ILogger<GetCompanySessionStatsHandler> logger
    )
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<CompanySessionStatsDTO>> Handle(
        GetCompanySessionStatsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // 1. Verificar que el usuario existe y obtener su compañía
            var userCompanyQuery =
                from u in _context.TaxUsers
                where u.Id == request.RequestingUserId && u.IsActive
                select new
                {
                    u.Id,
                    u.CompanyId,
                    u.IsOwner,
                };

            var userInfo = await userCompanyQuery.FirstOrDefaultAsync(cancellationToken);

            if (userInfo == null)
            {
                _logger.LogWarning("User not found: {RequesterId}", request.RequestingUserId);
                return new ApiResponse<CompanySessionStatsDTO>(
                    false,
                    "User not found",
                    new CompanySessionStatsDTO()
                );
            }

            // 2. Calcular fechas según el rango solicitado
            var (startDate, endDate) = CalculateDateRange(request.TimeRange);
            var now = DateTime.UtcNow;

            // 3. Query principal para obtener sesiones de la empresa
            var companySessionsQuery =
                from s in _context.Sessions
                join u in _context.TaxUsers on s.TaxUserId equals u.Id
                where u.CompanyId == userInfo.CompanyId && s.CreatedAt >= startDate
                select new { Session = s, User = u };

            var companySessions = await companySessionsQuery.ToListAsync(cancellationToken);

            // 4. Estadísticas básicas
            var activeSessions = companySessions
                .Where(cs => !cs.Session.IsRevoke && cs.Session.ExpireTokenRequest > now)
                .ToList();
            var totalUsers = companySessions.Select(cs => cs.User.Id).Distinct().Count();
            var activeUsers = activeSessions.Select(cs => cs.User.Id).Distinct().Count();

            // 5. Estadísticas por período
            var last24Hours = now.AddHours(-24);
            var last7Days = now.AddDays(-7);
            var last30Days = now.AddDays(-30);

            var sessionsLast24Hours = companySessions.Count(cs =>
                cs.Session.CreatedAt >= last24Hours
            );
            var sessionsLast7Days = companySessions.Count(cs => cs.Session.CreatedAt >= last7Days);
            var sessionsLast30Days = companySessions.Count(cs =>
                cs.Session.CreatedAt >= last30Days
            );

            // 6. Top ubicaciones
            var topLocations = companySessions
                .Where(cs => !string.IsNullOrEmpty(cs.Session.IpAddress))
                .GroupBy(cs => cs.Session.IpAddress)
                .Select(g => new LocationStats
                {
                    Location = g.Key ?? "Unknown",
                    SessionCount = g.Count(),
                    UserCount = g.Select(x => x.User.Id).Distinct().Count(),
                    // En un escenario real, aquí harías geocoding del IP
                    Country = "Unknown",
                    City = "Unknown",
                })
                .OrderByDescending(l => l.SessionCount)
                .Take(10)
                .ToList();

            // 7. Top dispositivos
            var topDevices = companySessions
                .Where(cs => !string.IsNullOrEmpty(cs.Session.Device))
                .GroupBy(cs => cs.Session.Device)
                .Select(g => new DeviceStats
                {
                    Device = g.Key ?? "Unknown",
                    SessionCount = g.Count(),
                    UserCount = g.Select(x => x.User.Id).Distinct().Count(),
                    LastUsed = g.Max(x => x.Session.CreatedAt),
                })
                .OrderByDescending(d => d.SessionCount)
                .Take(10)
                .ToList();

            // 8. Actividad por hora (últimos 7 días)
            var activityByHour = companySessions
                .Where(cs => cs.Session.CreatedAt >= last7Days)
                .GroupBy(cs => cs.Session.CreatedAt.Hour)
                .ToDictionary(g => g.Key, g => g.Count());

            // Llenar horas faltantes con 0
            for (int hour = 0; hour < 24; hour++)
            {
                if (!activityByHour.ContainsKey(hour))
                    activityByHour[hour] = 0;
            }

            // 9. Actividad por día (últimos 30 días)
            var activityByDay = companySessions
                .Where(cs => cs.Session.CreatedAt >= last30Days)
                .GroupBy(cs => cs.Session.CreatedAt.Date.ToString("yyyy-MM-dd"))
                .ToDictionary(g => g.Key, g => g.Count());

            // 10. Crear DTO de respuesta
            var stats = new CompanySessionStatsDTO
            {
                CompanyId = userInfo.CompanyId,
                TotalActiveSessions = activeSessions.Count,
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                UniqueLocations = companySessions
                    .Select(cs => cs.Session.IpAddress)
                    .Where(ip => !string.IsNullOrEmpty(ip))
                    .Distinct()
                    .Count(),
                UniqueDevices = companySessions
                    .Select(cs => cs.Session.Device)
                    .Where(d => !string.IsNullOrEmpty(d))
                    .Distinct()
                    .Count(),
                SessionsLast24Hours = sessionsLast24Hours,
                SessionsLast7Days = sessionsLast7Days,
                SessionsLast30Days = sessionsLast30Days,
                TopLocations = topLocations,
                TopDevices = topDevices,
                ActivityByHour = activityByHour,
                ActivityByDay = activityByDay,
            };

            _logger.LogInformation(
                "Company session stats retrieved: CompanyId={CompanyId}, ActiveSessions={ActiveSessions}, TotalUsers={TotalUsers}",
                userInfo.CompanyId,
                stats.TotalActiveSessions,
                stats.TotalUsers
            );

            return new ApiResponse<CompanySessionStatsDTO>(
                true,
                "Company session statistics retrieved successfully",
                stats
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving company session stats: RequesterId={RequesterId}",
                request.RequestingUserId
            );
            return new ApiResponse<CompanySessionStatsDTO>(
                false,
                "An error occurred while retrieving session statistics",
                new CompanySessionStatsDTO()
            );
        }
    }

    private static (DateTime startDate, DateTime endDate) CalculateDateRange(string timeRange)
    {
        var now = DateTime.UtcNow;
        var endDate = now;

        return timeRange.ToLower() switch
        {
            "24h" or "day" => (now.AddHours(-24), endDate),
            "7d" or "week" => (now.AddDays(-7), endDate),
            "30d" or "month" => (now.AddDays(-30), endDate),
            "90d" or "quarter" => (now.AddDays(-90), endDate),
            "1y" or "year" => (now.AddDays(-365), endDate),
            _ => (now.AddDays(-30), endDate), // Default to 30 days
        };
    }
}
