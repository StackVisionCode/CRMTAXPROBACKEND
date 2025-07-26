using Domain;

namespace Infrastructure.Services;

public interface IEmailSyncService
{
    Task<EmailSyncResult> SyncEmailsAsync(Guid configId, DateTime? since = null);
}
