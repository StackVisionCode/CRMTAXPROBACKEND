namespace Infrastructure.Services;

public interface IEmailSyncService
{
    Task<EmailSyncResult> SyncEmailsAsync(Guid configId, Guid companyId, DateTime? since = null);
}
