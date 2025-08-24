namespace SharedLibrary.DTOs.Reminders;

public sealed record ScheduleEventRemindersRequested(
    Guid Id,
    DateTime OccurredOn,
    Guid EventId,
    DateTimeOffset EventStartUtc,
    int[] DaysBefore,
    TimeSpan? RemindAtTime,
    string? Message,
    string Channel,
    string? UserId
) : IntegrationEvent(Id, OccurredOn);
