using AuthService.DTOs.InvitationDTOs;
using Common;
using Infraestructure.Context;
using Infraestructure.Queries.InvitationsQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.InvitationHandlers;

/// <summary>
/// Handler para obtener estadísticas globales de invitaciones (Developer)
/// </summary>
public class GetGlobalInvitationStatsHandler
    : IRequestHandler<GetGlobalInvitationStatsQuery, ApiResponse<List<InvitationStatsDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetGlobalInvitationStatsHandler> _logger;

    public GetGlobalInvitationStatsHandler(
        ApplicationDbContext dbContext,
        ILogger<GetGlobalInvitationStatsHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<List<InvitationStatsDTO>>> Handle(
        GetGlobalInvitationStatsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var daysBack = request.DaysBack;
            var cutoffDate = DateTime.UtcNow.AddDays(-daysBack);

            // 1. Estadísticas por company
            var companyStatsQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                select new
                {
                    CompanyId = c.Id,
                    CompanyName = c.IsCompany ? c.CompanyName : c.FullName,
                    CompanyDomain = c.Domain,
                    UserLimit = cp.UserLimit,
                    CurrentActiveUsers = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && u.IsActive
                    ),

                    // Estadísticas de invitaciones
                    TotalInvitations = _dbContext.Invitations.Count(i => i.CompanyId == c.Id),
                    PendingInvitations = _dbContext.Invitations.Count(i =>
                        i.CompanyId == c.Id && i.Status == InvitationStatus.Pending
                    ),
                    AcceptedInvitations = _dbContext.Invitations.Count(i =>
                        i.CompanyId == c.Id && i.Status == InvitationStatus.Accepted
                    ),
                    CancelledInvitations = _dbContext.Invitations.Count(i =>
                        i.CompanyId == c.Id && i.Status == InvitationStatus.Cancelled
                    ),
                    ExpiredInvitations = _dbContext.Invitations.Count(i =>
                        i.CompanyId == c.Id && i.Status == InvitationStatus.Expired
                    ),
                    FailedInvitations = _dbContext.Invitations.Count(i =>
                        i.CompanyId == c.Id && i.Status == InvitationStatus.Failed
                    ),

                    // Estadísticas por tiempo
                    InvitationsLast24Hours = _dbContext.Invitations.Count(i =>
                        i.CompanyId == c.Id && i.CreatedAt >= DateTime.UtcNow.AddDays(-1)
                    ),
                    InvitationsLast7Days = _dbContext.Invitations.Count(i =>
                        i.CompanyId == c.Id && i.CreatedAt >= DateTime.UtcNow.AddDays(-7)
                    ),
                    InvitationsLast30Days = _dbContext.Invitations.Count(i =>
                        i.CompanyId == c.Id && i.CreatedAt >= DateTime.UtcNow.AddDays(-30)
                    ),
                };

            var companyStats = await companyStatsQuery.ToListAsync(cancellationToken);

            // 2. Construir resultado
            var results = companyStats
                .Select(cs =>
                {
                    return new InvitationStatsDTO
                    {
                        CompanyId = cs.CompanyId,
                        CompanyName = cs.CompanyName ?? string.Empty, // ✅ Fix nullable
                        CompanyDomain = cs.CompanyDomain,

                        CustomPlanUserLimit = cs.UserLimit,
                        CurrentActiveUsers = cs.CurrentActiveUsers,

                        TotalInvitationsSent = cs.TotalInvitations,
                        PendingInvitations = cs.PendingInvitations,
                        AcceptedInvitations = cs.AcceptedInvitations,
                        CancelledInvitations = cs.CancelledInvitations,
                        ExpiredInvitations = cs.ExpiredInvitations,
                        FailedInvitations = cs.FailedInvitations,

                        InvitationsLast24Hours = cs.InvitationsLast24Hours,
                        InvitationsLast7Days = cs.InvitationsLast7Days,
                        InvitationsLast30Days = cs.InvitationsLast30Days,

                        // TopInviters se puede poblar con consulta adicional si se necesita
                        TopInviters = new List<InviterStats>(),
                    };
                })
                .OrderByDescending(r => r.TotalInvitationsSent)
                .ToList();

            _logger.LogDebug(
                "Generated global invitation stats for {CompanyCount} companies",
                results.Count
            );

            return new ApiResponse<List<InvitationStatsDTO>>(
                true,
                "Global invitation statistics retrieved successfully",
                results
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving global invitation statistics");
            return new ApiResponse<List<InvitationStatsDTO>>(
                false,
                ex.Message,
                new List<InvitationStatsDTO>()
            );
        }
    }
}
