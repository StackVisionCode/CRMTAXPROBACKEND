namespace Infrastructure.Services;

public interface IEmailStatisticsService
{
    Task<EmailStatistics> GetStatisticsAsync(
        Guid companyId,
        Guid? taxUserId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null
    );
    Task<DailyEmailStats[]> GetDailyStatsAsync(
        Guid companyId,
        Guid? taxUserId,
        DateTime fromDate,
        DateTime toDate
    );
}

public class EmailStatistics
{
    public int TotalSent { get; set; }
    public int TotalFailed { get; set; }
    public int TotalPending { get; set; }
    public int TotalReceived { get; set; }
    public int UnreadReceived { get; set; }
    public double SuccessRate { get; set; }
    public DateTime? LastEmailSent { get; set; }
    public DateTime? LastEmailReceived { get; set; }
}

public class DailyEmailStats
{
    public DateTime Date { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
    public int Received { get; set; }
}
