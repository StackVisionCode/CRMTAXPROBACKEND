using AuthService.DTOs.CompanyPermissionDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyPermissionQueries;

namespace AuthService.Handlers.CompanyPermissionHandlers;

/// Handler para obtener estadísticas de permisos por Company
public class GetCompanyPermissionStatsHandler
    : IRequestHandler<GetCompanyPermissionStatsQuery, ApiResponse<CompanyPermissionStatsDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetCompanyPermissionStatsHandler> _logger;

    public GetCompanyPermissionStatsHandler(
        ApplicationDbContext dbContext,
        ILogger<GetCompanyPermissionStatsHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CompanyPermissionStatsDTO>> Handle(
        GetCompanyPermissionStatsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Total de usuarios en la company
            var totalUsers = await _dbContext
                .TaxUsers.Where(tu => tu.CompanyId == request.CompanyId && tu.IsActive)
                .CountAsync(cancellationToken);

            // Usuarios con permisos personalizados
            var usersWithCustomPermissions = await (
                from cp in _dbContext.CompanyPermissions
                join tu in _dbContext.TaxUsers on cp.TaxUserId equals tu.Id
                where tu.CompanyId == request.CompanyId
                select cp.TaxUserId
            )
                .Distinct()
                .CountAsync(cancellationToken);

            // Estadísticas de permisos
            var permissionStats = await (
                from cp in _dbContext.CompanyPermissions
                join tu in _dbContext.TaxUsers on cp.TaxUserId equals tu.Id
                join p in _dbContext.Permissions on cp.PermissionId equals p.Id
                where tu.CompanyId == request.CompanyId
                group cp by new { cp.IsGranted, p.Code } into g
                select new
                {
                    g.Key.IsGranted,
                    g.Key.Code,
                    Count = g.Count(),
                }
            ).ToListAsync(cancellationToken);

            var totalCustomPermissions = permissionStats.Sum(ps => ps.Count);
            var grantedPermissions = permissionStats.Where(ps => ps.IsGranted).Sum(ps => ps.Count);
            var revokedPermissions = permissionStats.Where(ps => !ps.IsGranted).Sum(ps => ps.Count);

            // Permisos más usados
            var mostUsedPermissions = permissionStats
                .GroupBy(ps => ps.Code)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));

            var stats = new CompanyPermissionStatsDTO
            {
                CompanyId = request.CompanyId,
                TotalUsers = totalUsers,
                UsersWithCustomPermissions = usersWithCustomPermissions,
                TotalCustomPermissions = totalCustomPermissions,
                GrantedPermissions = grantedPermissions,
                RevokedPermissions = revokedPermissions,
                MostUsedPermissions = mostUsedPermissions,
            };

            return new ApiResponse<CompanyPermissionStatsDTO>(
                true,
                "Company permission statistics retrieved successfully",
                stats
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting company permission stats: {CompanyId}",
                request.CompanyId
            );
            return new ApiResponse<CompanyPermissionStatsDTO>(
                false,
                "Error retrieving company permission statistics",
                null!
            );
        }
    }
}
