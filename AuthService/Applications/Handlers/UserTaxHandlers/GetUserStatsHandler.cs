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
            // Usar CustomPlan.UserLimit directamente
            var statsQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                where c.Id == request.CompanyId
                select new
                {
                    Company = c,
                    CustomPlan = cp,
                    // User counts - MEJORADO
                    TotalUsers = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id),
                    ActiveUsers = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id && u.IsActive),
                    OwnerCount = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id && u.IsOwner),
                    RegularUserCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && !u.IsOwner
                    ),
                    ConfirmedUsers = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && u.Confirm == true
                    ),
                    // Usar CustomPlan.UserLimit directamente
                    PlanUserLimit = cp.UserLimit,
                    // Last user created
                    LastUserCreated = _dbContext.TaxUsers.Where(u => u.CompanyId == c.Id).Any()
                        ? _dbContext.TaxUsers.Where(u => u.CompanyId == c.Id).Max(u => u.CreatedAt)
                        : DateTime.MinValue,
                };

            var statsData = await statsQuery.FirstOrDefaultAsync(cancellationToken);
            if (statsData?.Company == null)
            {
                return new ApiResponse<UserStatsDTO>(false, "Company not found", null!);
            }

            // Users by role query (sin cambios)
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

            // Lógica de estadísticas mejorada
            var stats = new UserStatsDTO
            {
                CompanyId = request.CompanyId,
                TotalUsers = statsData.TotalUsers,
                ActiveUsers = statsData.ActiveUsers,
                InactiveUsers = statsData.TotalUsers - statsData.ActiveUsers,
                OwnerCount = statsData.OwnerCount,
                RegularUserCount = statsData.RegularUserCount,
                ConfirmedUsers = statsData.ConfirmedUsers,
                PendingConfirmation = statsData.TotalUsers - statsData.ConfirmedUsers,

                // Usar CustomPlan.UserLimit como autoridad
                PlanUserLimit = statsData.PlanUserLimit,

                // Calcular slots disponibles considerando todos los usuarios activos
                AvailableSlots = Math.Max(0, statsData.PlanUserLimit - statsData.ActiveUsers),

                // Información adicional útil
                IsWithinLimits = statsData.ActiveUsers <= statsData.PlanUserLimit,
                UsagePercentage =
                    statsData.PlanUserLimit > 0
                        ? (int)
                            Math.Round(
                                (double)statsData.ActiveUsers / statsData.PlanUserLimit * 100
                            )
                        : 0,

                UsersByRole = usersByRole,
                LastUserCreated = statsData.LastUserCreated,
            };

            _logger.LogInformation(
                "User stats retrieved for company {CompanyId}: {Active}/{Total} users, Limit: {Limit}, Available: {Available}",
                request.CompanyId,
                statsData.ActiveUsers,
                statsData.TotalUsers,
                statsData.PlanUserLimit,
                stats.AvailableSlots
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
