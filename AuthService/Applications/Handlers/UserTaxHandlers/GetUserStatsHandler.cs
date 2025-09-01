using AuthService.Applications.Common;
using AuthService.DTOs.UserDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.UserQueries;

namespace Handlers.UserHandlers;

public class GetUserStatsHandler : IRequestHandler<GetUserStatsQuery, ApiResponse<UserStatsDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetUserStatsHandler> _logger;

    public GetUserStatsHandler(ApplicationDbContext dbContext, ILogger<GetUserStatsHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<UserStatsDTO>> Handle(
        GetUserStatsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Query simplificado - solo datos de AuthService
            var statsQuery =
                from c in _dbContext.Companies
                where c.Id == request.CompanyId
                select new
                {
                    Company = c,
                    // Conteos de usuarios (disponibles en AuthService)
                    TotalUsers = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id),
                    ActiveUsers = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id && u.IsActive),
                    OwnerCount = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id && u.IsOwner),
                    RegularUserCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && !u.IsOwner
                    ),
                    ConfirmedUsers = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && u.Confirm == true
                    ),
                    // Último usuario creado
                    LastUserCreated = _dbContext.TaxUsers.Where(u => u.CompanyId == c.Id).Any()
                        ? _dbContext.TaxUsers.Where(u => u.CompanyId == c.Id).Max(u => u.CreatedAt)
                        : DateTime.MinValue,
                };

            var statsData = await statsQuery.FirstOrDefaultAsync(cancellationToken);
            if (statsData?.Company == null)
            {
                return new ApiResponse<UserStatsDTO>(false, "Company not found", null!);
            }

            // Users by role query
            var usersByRoleQuery =
                from u in _dbContext.TaxUsers
                join ur in _dbContext.UserRoles on u.Id equals ur.TaxUserId
                join r in _dbContext.Roles on ur.RoleId equals r.Id
                where u.CompanyId == request.CompanyId
                group u by r.Name into g
                select new { RoleName = g.Key, Count = g.Count() };

            var usersByRole = await usersByRoleQuery.ToDictionaryAsync(
                x => x.RoleName,
                x => x.Count,
                cancellationToken
            );

            // Estadísticas enfocadas en AuthService
            var stats = new UserStatsDTO
            {
                CompanyId = request.CompanyId,
                CompanyServiceLevel = statsData.Company.ServiceLevel, // NUEVO

                // Conteos de usuarios (disponibles en AuthService)
                TotalUsers = statsData.TotalUsers,
                ActiveUsers = statsData.ActiveUsers,
                InactiveUsers = statsData.TotalUsers - statsData.ActiveUsers,
                OwnerCount = statsData.OwnerCount,
                RegularUserCount = statsData.RegularUserCount,
                ConfirmedUsers = statsData.ConfirmedUsers,
                PendingConfirmation = statsData.TotalUsers - statsData.ConfirmedUsers,

                UsersByRole = usersByRole,
                LastUserCreated = statsData.LastUserCreated,

                // REMOVIDO - información de límites del plan (responsabilidad del frontend)
                // PlanUserLimit, AvailableSlots, IsWithinLimits, UsagePercentage
            };

            _logger.LogInformation(
                "User stats retrieved for company {CompanyId} (ServiceLevel: {ServiceLevel}): {Active}/{Total} users",
                request.CompanyId,
                statsData.Company.ServiceLevel,
                statsData.ActiveUsers,
                statsData.TotalUsers
            );

            return new ApiResponse<UserStatsDTO>(true, "Stats retrieved successfully", stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting user stats for company: {CompanyId}",
                request.CompanyId
            );
            return new ApiResponse<UserStatsDTO>(false, "Error retrieving stats", null!);
        }
    }
}
