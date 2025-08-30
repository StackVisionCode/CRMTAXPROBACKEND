using AuthService.DTOs.InvitationDTOs;
using Common;
using Infraestructure.Context;
using Infraestructure.Queries.InvitationsQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.InvitationHandlers;

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

            // Query principal - sin CustomPlans
            var companyStatsQuery =
                from c in _dbContext.Companies
                select new
                {
                    CompanyId = c.Id,
                    CompanyName = c.IsCompany ? c.CompanyName : c.FullName,
                    CompanyDomain = c.Domain,
                    ServiceLevel = c.ServiceLevel,
                    IsCompany = c.IsCompany,

                    // Estadísticas de usuarios (disponibles en AuthService)
                    CurrentActiveUsers = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && u.IsActive
                    ),
                    CurrentTotalUsers = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id),
                    OwnerCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && u.IsOwner && u.IsActive
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

            // Construir resultado
            var results = companyStats
                .Select(cs => new InvitationStatsDTO
                {
                    CompanyId = cs.CompanyId,
                    CompanyName = cs.CompanyName ?? string.Empty,
                    CompanyDomain = cs.CompanyDomain,
                    ServiceLevel = cs.ServiceLevel,
                    IsCompany = cs.IsCompany,

                    // Estadísticas de usuarios
                    CurrentActiveUsers = cs.CurrentActiveUsers,
                    CurrentTotalUsers = cs.CurrentTotalUsers,
                    OwnerCount = cs.OwnerCount,

                    // Estadísticas de invitaciones
                    TotalInvitationsSent = cs.TotalInvitations,
                    PendingInvitations = cs.PendingInvitations,
                    AcceptedInvitations = cs.AcceptedInvitations,
                    CancelledInvitations = cs.CancelledInvitations,
                    ExpiredInvitations = cs.ExpiredInvitations,
                    FailedInvitations = cs.FailedInvitations,

                    InvitationsLast24Hours = cs.InvitationsLast24Hours,
                    InvitationsLast7Days = cs.InvitationsLast7Days,
                    InvitationsLast30Days = cs.InvitationsLast30Days,

                    TopInviters = new List<InviterStats>(), // Se puede poblar después si es necesario
                    RequiresSubscriptionCheck = true,
                    GeneratedAt = DateTime.UtcNow,
                })
                .Where(r => r.TotalInvitationsSent > 0) // Solo companies con invitaciones
                .OrderByDescending(r => r.TotalInvitationsSent)
                .ToList();

            // Opcionalmente poblar TopInviters para companies con muchas invitaciones
            if (results.Any())
            {
                await PopulateTopInvitersForTopCompaniesAsync(
                    results.Take(10).ToList(),
                    cutoffDate,
                    cancellationToken
                );
            }

            _logger.LogInformation(
                "Generated global invitation stats for {CompanyCount} companies over {DaysBack} days",
                results.Count,
                daysBack
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

    private async Task PopulateTopInvitersForTopCompaniesAsync(
        List<InvitationStatsDTO> topCompanies,
        DateTime cutoffDate,
        CancellationToken cancellationToken
    )
    {
        var companyIds = topCompanies.Select(c => c.CompanyId).ToList();

        var topInvitersQuery =
            from i in _dbContext.Invitations
            join u in _dbContext.TaxUsers on i.InvitedByUserId equals u.Id
            where companyIds.Contains(i.CompanyId) && i.CreatedAt >= cutoffDate
            group i by new
            {
                CompanyId = i.CompanyId,
                UserId = u.Id,
                UserName = u.Name ?? string.Empty,
                UserLastName = u.LastName ?? string.Empty,
                UserEmail = u.Email,
                IsOwner = u.IsOwner,
            } into g
            select new
            {
                g.Key.CompanyId,
                g.Key.UserId,
                g.Key.UserName,
                g.Key.UserLastName,
                g.Key.UserEmail,
                g.Key.IsOwner,
                TotalSent = g.Count(),
                Accepted = g.Count(x => x.Status == InvitationStatus.Accepted),
                Pending = g.Count(x => x.Status == InvitationStatus.Pending),
                Cancelled = g.Count(x => x.Status == InvitationStatus.Cancelled),
            };

        var topInvitersData = await topInvitersQuery.ToListAsync(cancellationToken);
        var topInvitersByCompany = topInvitersData
            .GroupBy(t => t.CompanyId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.TotalSent).Take(3).ToList());

        // Asignar a cada company
        foreach (var company in topCompanies)
        {
            if (topInvitersByCompany.TryGetValue(company.CompanyId, out var inviters))
            {
                company.TopInviters = inviters
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
                    .ToList();
            }
        }
    }
}
