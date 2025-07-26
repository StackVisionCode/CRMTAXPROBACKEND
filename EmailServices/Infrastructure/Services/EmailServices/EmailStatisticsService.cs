using EmailServices.Domain;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class EmailStatisticsService : IEmailStatisticsService
{
    private readonly EmailContext _context;

    public EmailStatisticsService(EmailContext context)
    {
        _context = context;
    }

    public async Task<EmailStatistics> GetStatisticsAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null
    )
    {
        var outgoingQuery = _context.Emails.Where(e => e.SentByUserId == userId);
        var incomingQuery = _context.IncomingEmails.Where(e => e.UserId == userId);

        if (fromDate.HasValue)
        {
            outgoingQuery = outgoingQuery.Where(e => e.CreatedOn >= fromDate);
            incomingQuery = incomingQuery.Where(e => e.ReceivedOn >= fromDate);
        }

        if (toDate.HasValue)
        {
            outgoingQuery = outgoingQuery.Where(e => e.CreatedOn <= toDate);
            incomingQuery = incomingQuery.Where(e => e.ReceivedOn <= toDate);
        }

        var totalSent = await outgoingQuery.CountAsync(e => e.Status == EmailStatus.Sent);
        var totalFailed = await outgoingQuery.CountAsync(e => e.Status == EmailStatus.Failed);
        var totalPending = await outgoingQuery.CountAsync(e => e.Status == EmailStatus.Pending);
        var totalReceived = await incomingQuery.CountAsync();
        var unreadReceived = await incomingQuery.CountAsync(e => !e.IsRead);

        var lastEmailSent = await outgoingQuery
            .Where(e => e.Status == EmailStatus.Sent)
            .OrderByDescending(e => e.SentOn)
            .Select(e => e.SentOn)
            .FirstOrDefaultAsync();

        var lastEmailReceived = await incomingQuery
            .OrderByDescending(e => e.ReceivedOn)
            .Select(e => e.ReceivedOn)
            .FirstOrDefaultAsync();

        var successRate =
            totalSent + totalFailed > 0 ? (double)totalSent / (totalSent + totalFailed) * 100 : 0;

        return new EmailStatistics
        {
            TotalSent = totalSent,
            TotalFailed = totalFailed,
            TotalPending = totalPending,
            TotalReceived = totalReceived,
            UnreadReceived = unreadReceived,
            SuccessRate = Math.Round(successRate, 2),
            LastEmailSent = lastEmailSent,
            LastEmailReceived = lastEmailReceived,
        };
    }

    public async Task<DailyEmailStats[]> GetDailyStatsAsync(
        Guid userId,
        DateTime fromDate,
        DateTime toDate
    )
    {
        var outgoingStats = await _context
            .Emails.Where(e =>
                e.SentByUserId == userId && e.CreatedOn >= fromDate && e.CreatedOn <= toDate
            )
            .GroupBy(e => e.CreatedOn.Date)
            .Select(g => new
            {
                Date = g.Key,
                Sent = g.Count(e => e.Status == EmailStatus.Sent),
                Failed = g.Count(e => e.Status == EmailStatus.Failed),
            })
            .ToListAsync();

        var incomingStats = await _context
            .IncomingEmails.Where(e =>
                e.UserId == userId && e.ReceivedOn >= fromDate && e.ReceivedOn <= toDate
            )
            .GroupBy(e => e.ReceivedOn.Date)
            .Select(g => new { Date = g.Key, Received = g.Count() })
            .ToListAsync();

        var allDates = outgoingStats
            .Select(s => s.Date)
            .Union(incomingStats.Select(s => s.Date))
            .Distinct()
            .OrderBy(d => d);

        return allDates
            .Select(date => new DailyEmailStats
            {
                Date = date,
                Sent = outgoingStats.FirstOrDefault(s => s.Date == date)?.Sent ?? 0,
                Failed = outgoingStats.FirstOrDefault(s => s.Date == date)?.Failed ?? 0,
                Received = incomingStats.FirstOrDefault(s => s.Date == date)?.Received ?? 0,
            })
            .ToArray();
    }
}
