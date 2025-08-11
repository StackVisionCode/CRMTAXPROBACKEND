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
            // Query para estadísticas completas
            var statsQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                where c.Id == request.CompanyId
                select new
                {
                    Company = c,
                    CustomPlan = cp,
                    // Conteos de usuarios
                    TaxUsersTotal = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id),
                    TaxUsersActive = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && u.IsActive
                    ),
                    EmployeesTotal = _dbContext.UserCompanies.Count(uc => uc.CompanyId == c.Id),
                    EmployeesActive = _dbContext.UserCompanies.Count(uc =>
                        uc.CompanyId == c.Id && uc.IsActive
                    ),
                    // Sesiones activas
                    ActiveSessions = (
                        from s in _dbContext.Sessions
                        join u in _dbContext.TaxUsers on s.TaxUserId equals u.Id
                        where u.CompanyId == c.Id && !s.IsRevoke
                        select s.Id
                    ).Count()
                        + (
                            from ucs in _dbContext.UserCompanySessions
                            join uc in _dbContext.UserCompanies on ucs.UserCompanyId equals uc.Id
                            where uc.CompanyId == c.Id && !ucs.IsRevoke
                            select ucs.Id
                        ).Count(),
                };

            var stats = await statsQuery.FirstOrDefaultAsync(cancellationToken);
            if (stats?.Company == null)
            {
                return new ApiResponse<CompanyStatsDTO>(false, "Company not found", null!);
            }

            // Obtener límite del servicio
            var serviceLimitQuery =
                from s in _dbContext.Services
                from cm in _dbContext.CustomModules
                join m in _dbContext.Modules on cm.ModuleId equals m.Id
                where cm.CustomPlanId == stats.CustomPlan.Id && m.ServiceId == s.Id && cm.IsIncluded
                select s.UserLimit;

            var serviceLimit = await serviceLimitQuery.FirstOrDefaultAsync(cancellationToken);

            // Crear respuesta
            var result = new CompanyStatsDTO
            {
                CompanyId = stats.Company.Id,
                CompanyName = stats.Company.IsCompany
                    ? stats.Company.CompanyName
                    : stats.Company.FullName,
                Domain = stats.Company.Domain,
                IsCompany = stats.Company.IsCompany,

                // Estadísticas de preparadores (TaxUsers)
                TotalPreparers = stats.TaxUsersTotal,
                ActivePreparers = stats.TaxUsersActive,

                // Estadísticas de empleados (UserCompanies)
                TotalEmployees = stats.EmployeesTotal,
                ActiveEmployees = stats.EmployeesActive,

                // Plan información
                CustomPlanPrice = stats.CustomPlan.Price,
                CustomPlanIsActive = stats.CustomPlan.IsActive,
                ServiceUserLimit = serviceLimit,
                IsWithinLimits = (stats.EmployeesTotal <= serviceLimit),

                // Actividad
                ActiveSessions = stats.ActiveSessions,

                CreatedAt = stats.Company.CreatedAt,
            };

            _logger.LogInformation(
                "Retrieved stats for company {CompanyId}: {Preparers} preparers, {Employees} employees",
                request.CompanyId,
                result.TotalPreparers,
                result.TotalEmployees
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
