using Domain;

namespace Infrastructure.Services;

public interface IReactiveEmailReceivingService
{
    Task<IEnumerable<IncomingEmail>> CheckAllEmailsAsync(
        EmailConfig config,
        int maxMessages = 100,
        DateTime? since = null
    );
    Task StartWatchingAsync(EmailConfig config);
    Task StopWatchingAsync(Guid configId);
    Task<EmailSyncResult> SyncAllEmailsAsync(Guid configId, Guid companyId, DateTime? since = null);
}

public class EmailSyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalFetched { get; set; }
    public int NewEmails { get; set; }
    public int ExistingEmails { get; set; }
    public DateTime SyncTime { get; set; } = DateTime.UtcNow;
    public Guid ConfigId { get; set; }
    public string ConfigName { get; set; } = string.Empty;
}
