using AuthService.DTOs.InvitationDTOs;
using Common;
using Infraestructure.Context;
using Infraestructure.Queries.InvitationsQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.InvitationHandlers;

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

            // Información básica de la company (sin CustomPlans)
            var companyInfoQuery =
                from c in _dbContext.Companies
                where c.Id == companyId
                select new
                {
                    c.Id,
                    CompanyName = c.IsCompany ? c.CompanyName : c.FullName,
                    c.Domain,
                    c.ServiceLevel,
                    c.IsCompany,
                    CurrentActiveUsers = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == companyId && u.IsActive
                    ),
                    CurrentTotalUsers = _dbContext.TaxUsers.Count(u => u.CompanyId == companyId),
                    OwnerCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == companyId && u.IsOwner && u.IsActive
                    ),
                };

            var companyInfo = await companyInfoQuery.FirstOrDefaultAsync(cancellationToken);
            if (companyInfo == null)
            {
                _logger.LogWarning("Company not found for stats: {CompanyId}", companyId);
                return new ApiResponse<InvitationStatsDTO>(false, "Company not found", null!);
            }

            // Estadísticas generales de invitaciones por status
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

            // Estadísticas por período de tiempo
            var now = DateTime.UtcNow;
            var timeBasedStatsQuery =
                from i in _dbContext.Invitations
                where i.CompanyId == companyId
                select new
                {
                    IsLast24Hours = i.CreatedAt >= now.AddDays(-1),
                    IsLast7Days = i.CreatedAt >= now.AddDays(-7),
                    IsLast30Days = i.CreatedAt >= now.AddDays(-30),
                };

            var timeBasedStats = await timeBasedStatsQuery.ToListAsync(cancellationToken);

            // Top usuarios que más invitan
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

            // Construir resultado
            var totalInvitations = invitationStats.Values.Sum();
            var result = new InvitationStatsDTO
            {
                CompanyId = companyId,
                CompanyName = companyInfo.CompanyName,
                CompanyDomain = companyInfo.Domain,
                ServiceLevel = companyInfo.ServiceLevel,
                IsCompany = companyInfo.IsCompany,

                // Estadísticas de usuarios
                CurrentActiveUsers = companyInfo.CurrentActiveUsers,
                CurrentTotalUsers = companyInfo.CurrentTotalUsers,
                OwnerCount = companyInfo.OwnerCount,

                // Estadísticas de invitaciones
                TotalInvitationsSent = totalInvitations,
                PendingInvitations = invitationStats.GetValueOrDefault(InvitationStatus.Pending, 0),
                AcceptedInvitations = invitationStats.GetValueOrDefault(
                    InvitationStatus.Accepted,
                    0
                ),
                CancelledInvitations = invitationStats.GetValueOrDefault(
                    InvitationStatus.Cancelled,
                    0
                ),
                ExpiredInvitations = invitationStats.GetValueOrDefault(InvitationStatus.Expired, 0),
                FailedInvitations = invitationStats.GetValueOrDefault(InvitationStatus.Failed, 0),

                InvitationsLast24Hours = timeBasedStats.Count(x => x.IsLast24Hours),
                InvitationsLast7Days = timeBasedStats.Count(x => x.IsLast7Days),
                InvitationsLast30Days = timeBasedStats.Count(x => x.IsLast30Days),

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

                RequiresSubscriptionCheck = true,
                GeneratedAt = DateTime.UtcNow,
            };

            _logger.LogInformation(
                "Generated invitation stats for company {CompanyId} (ServiceLevel: {ServiceLevel}): "
                    + "{TotalInvitations} total, {PendingInvitations} pending, {AcceptanceRate:F1}% acceptance rate",
                companyId,
                companyInfo.ServiceLevel,
                totalInvitations,
                result.PendingInvitations,
                result.AcceptanceRate
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
