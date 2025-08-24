namespace Infrastructure.Reminders;

public interface IReminderClient
{
    // Compat con tu handler actual
    Task ScheduleForEvent(Guid eventId, int[] daysBefore, CancellationToken ct = default);

    // Versión completa (recomendada)
    Task ScheduleForEvent(
        Guid eventId,
        DateTimeOffset eventStartUtc,
        int[] daysBefore,
        TimeSpan? remindAtTime = null,
        string? message = null,
        string channel = "email",
        string? userId = null,
        CancellationToken ct = default
    );
}
