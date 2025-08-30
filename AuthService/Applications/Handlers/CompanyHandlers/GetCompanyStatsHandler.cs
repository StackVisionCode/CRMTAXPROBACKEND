using AuthService.DTOs.CompanyDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyQueries;

namespace Handlers.CompanyHandlers;

public class GetCompanyStatsHandler
    : IRequestHandler<GetCompanyStatsQuery, ApiResponse<CompanyStatsDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetCompanyStatsHandler> _logger;

    public GetCompanyStatsHandler(
        ApplicationDbContext dbContext,
        ILogger<GetCompanyStatsHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CompanyStatsDTO>> Handle(
        GetCompanyStatsQuery request,
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
                    // Conteos de TaxUsers
                    TaxUsersTotal = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id),
                    TaxUsersActive = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && u.IsActive
                    ),
                    OwnerCount = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id && u.IsOwner),
                    RegularUsersCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && !u.IsOwner
                    ),

                    // Sesiones activas de TaxUsers
                    ActiveSessions = (
                        from s in _dbContext.Sessions
                        join u in _dbContext.TaxUsers on s.TaxUserId equals u.Id
                        where u.CompanyId == c.Id && !s.IsRevoke
                        select s.Id
                    ).Count(),
                };

            var stats = await statsQuery.FirstOrDefaultAsync(cancellationToken);
            if (stats?.Company == null)
            {
                _logger.LogWarning("Company not found: {CompanyId}", request.CompanyId);
                return new ApiResponse<CompanyStatsDTO>(false, "Company not found", null!);
            }

            // Crear resultado solo con datos de AuthService
            var result = new CompanyStatsDTO
            {
                CompanyId = stats.Company.Id,
                CompanyName = stats.Company.IsCompany
                    ? stats.Company.CompanyName
                    : stats.Company.FullName,
                Domain = stats.Company.Domain,
                IsCompany = stats.Company.IsCompany,
                ServiceLevel = stats.Company.ServiceLevel, // NUEVO

                // Estadísticas de usuarios (disponibles en AuthService)
                TotalUsers = stats.TaxUsersTotal,
                ActiveUsers = stats.TaxUsersActive,
                OwnerCount = stats.OwnerCount,
                RegularUsers = stats.RegularUsersCount,

                // Actividad
                ActiveSessions = stats.ActiveSessions,
                CreatedAt = stats.Company.CreatedAt,

                // REMOVIDO - datos de CustomPlan
                // El frontend obtendrá estos datos de SubscriptionsService:
                // - CustomPlanPrice
                // - ServiceUserLimit
                // - IsWithinLimits
            };

            _logger.LogInformation(
                "Retrieved stats for company {CompanyId}: ServiceLevel={ServiceLevel}, "
                    + "{Total} TaxUsers ({Owner} Owner, {Regular} Regular), {Sessions} active sessions",
                request.CompanyId,
                stats.Company.ServiceLevel,
                result.TotalUsers,
                result.OwnerCount,
                result.RegularUsers,
                result.ActiveSessions
            );

            return new ApiResponse<CompanyStatsDTO>(
                true,
                "Company statistics retrieved successfully",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving stats for company {CompanyId}: {Message}",
                request.CompanyId,
                ex.Message
            );
            return new ApiResponse<CompanyStatsDTO>(false, ex.Message, null!);
        }
    }
}
