using Domain.Entities;

namespace Infrastructure.Services;

public interface IReminderSender
{
    Task SendAsync(Reminder reminder, CancellationToken ct = default);
}
