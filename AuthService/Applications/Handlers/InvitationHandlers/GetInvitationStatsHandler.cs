using AuthService.DTOs.InvitationDTOs;
using Common;
using Infraestructure.Context;
using Infraestructure.Queries.InvitationsQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.InvitationHandlers;

/// <summary>
/// Handler para obtener estadísticas de invitaciones de una company
/// </summary>
public class GetInvitationStatsHandler
    : IRequestHandler<GetInvitationStatsQuery, ApiResponse<InvitationStatsDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetInvitationStatsHandler> _logger;

    public GetInvitationStatsHandler(
        ApplicationDbContext dbContext,
        ILogger<GetInvitationStatsHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<InvitationStatsDTO>> Handle(
        GetInvitationStatsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var companyId = request.CompanyId;
            var daysBack = request.DaysBack;
            var cutoffDate = DateTime.UtcNow.AddDays(-daysBack);

            // 1. Información básica de la company y límites
            var companyInfoQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                where c.Id == companyId
                select new
                {
                    c.Id,
                    CompanyName = c.IsCompany ? c.CompanyName : c.FullName,
                    c.Domain,
                    UserLimit = cp.UserLimit,
                    CurrentActiveUsers = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == companyId && u.IsActive
                    ),
                };

            var companyInfo = await companyInfoQuery.FirstOrDefaultAsync(cancellationToken);
            if (companyInfo == null)
            {
                _logger.LogWarning("Company not found for stats: {CompanyId}", companyId);
                return new ApiResponse<InvitationStatsDTO>(false, "Company not found", null!);
            }

            // 2. Estadísticas generales de invitaciones
            var invitationStatsQuery =
                from i in _dbContext.Invitations
                where i.CompanyId == companyId
                group i by i.Status into g
                select new { Status = g.Key, Count = g.Count() };

            var invitationStats = await invitationStatsQuery.ToDictionaryAsync(
                x => x.Status,
                x => x.Count,
                cancellationToken
            );

            // 3. Estadísticas por período de tiempo
            var now = DateTime.UtcNow;
            var last24Hours = now.AddDays(-1);
            var last7Days = now.AddDays(-7);
            var last30Days = now.AddDays(-30);

            var timeBasedStats = await (
                from i in _dbContext.Invitations
                where i.CompanyId == companyId
                select new
                {
                    InvitationId = i.Id,
                    CreatedAt = i.CreatedAt,
                    IsLast24Hours = i.CreatedAt >= last24Hours,
                    IsLast7Days = i.CreatedAt >= last7Days,
                    IsLast30Days = i.CreatedAt >= last30Days,
                }
            ).ToListAsync(cancellationToken);

            var invitationsLast24Hours = timeBasedStats.Count(x => x.IsLast24Hours);
            var invitationsLast7Days = timeBasedStats.Count(x => x.IsLast7Days);
            var invitationsLast30Days = timeBasedStats.Count(x => x.IsLast30Days);

            // 4. Top usuarios que más invitan
            var topInvitersQuery =
                from i in _dbContext.Invitations
                join u in _dbContext.TaxUsers on i.InvitedByUserId equals u.Id
                where i.CompanyId == companyId && i.CreatedAt >= cutoffDate
                group i by new
                {
                    u.Id,
                    u.Name,
                    u.LastName,
                    u.Email,
                    u.IsOwner,
                } into g
                select new
                {
                    UserId = g.Key.Id,
                    UserName = g.Key.Name ?? string.Empty,
                    UserLastName = g.Key.LastName ?? string.Empty,
                    UserEmail = g.Key.Email,
                    IsOwner = g.Key.IsOwner,
                    TotalSent = g.Count(),
                    Accepted = g.Count(x => x.Status == InvitationStatus.Accepted),
                    Pending = g.Count(x => x.Status == InvitationStatus.Pending),
                    Cancelled = g.Count(x => x.Status == InvitationStatus.Cancelled),
                };

            var topInviters = await topInvitersQuery
                .OrderByDescending(x => x.TotalSent)
                .Take(10)
                .ToListAsync(cancellationToken);

            // 5. Construir resultado
            var totalInvitations = invitationStats.Values.Sum();
            var pendingInvitations = invitationStats.GetValueOrDefault(InvitationStatus.Pending, 0);
            var acceptedInvitations = invitationStats.GetValueOrDefault(
                InvitationStatus.Accepted,
                0
            );
            var cancelledInvitations = invitationStats.GetValueOrDefault(
                InvitationStatus.Cancelled,
                0
            );
            var expiredInvitations = invitationStats.GetValueOrDefault(InvitationStatus.Expired, 0);
            var failedInvitations = invitationStats.GetValueOrDefault(InvitationStatus.Failed, 0);

            var result = new InvitationStatsDTO
            {
                CompanyId = companyId,
                CompanyName = companyInfo.CompanyName,
                CompanyDomain = companyInfo.Domain,

                CustomPlanUserLimit = companyInfo.UserLimit,
                CurrentActiveUsers = companyInfo.CurrentActiveUsers,

                TotalInvitationsSent = totalInvitations,
                PendingInvitations = pendingInvitations,
                AcceptedInvitations = acceptedInvitations,
                CancelledInvitations = cancelledInvitations,
                ExpiredInvitations = expiredInvitations,
                FailedInvitations = failedInvitations,

                InvitationsLast24Hours = invitationsLast24Hours,
                InvitationsLast7Days = invitationsLast7Days,
                InvitationsLast30Days = invitationsLast30Days,

                TopInviters = topInviters
                    .Select(t => new InviterStats
                    {
                        UserId = t.UserId,
                        UserName = t.UserName,
                        UserLastName = t.UserLastName,
                        UserEmail = t.UserEmail,
                        IsOwner = t.IsOwner,
                        TotalInvitationsSent = t.TotalSent,
                        AcceptedInvitations = t.Accepted,
                        PendingInvitations = t.Pending,
                        CancelledInvitations = t.Cancelled,
                    })
                    .ToList(),
            };

            _logger.LogDebug(
                "Generated invitation stats for company {CompanyId}: {TotalInvitations} total, {PendingInvitations} pending",
                companyId,
                totalInvitations,
                pendingInvitations
            );

            return new ApiResponse<InvitationStatsDTO>(
                true,
                "Invitation statistics retrieved successfully",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving invitation statistics for CompanyId: {CompanyId}",
                request.CompanyId
            );
            return new ApiResponse<InvitationStatsDTO>(false, ex.Message, null!);
        }
    }
}
