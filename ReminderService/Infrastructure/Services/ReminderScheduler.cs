using Domain.Entities;
using Infrastructure.Context;
using Jobs;
using Quartz;

namespace Infrastructure.Services;

public class ReminderScheduler
{
    private readonly ReminderDbContext _db;
    private readonly ISchedulerFactory _schedulerFactory;

    public ReminderScheduler(ReminderDbContext db, ISchedulerFactory schedulerFactory)
    {
        _db = db;
        _schedulerFactory = schedulerFactory;
    }

    public async Task<Reminder> ScheduleOneShotAsync(Reminder reminder, CancellationToken ct = default)
    {
        _db.Reminders.Add(reminder);
        await _db.SaveChangesAsync(ct);

        var scheduler = await _schedulerFactory.GetScheduler(ct);

        var job = JobBuilder.Create<ReminderJob>()
            .WithIdentity($"reminder-job-{reminder.Id}")
            .UsingJobData("reminderId", reminder.Id.ToString())
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"reminder-trigger-{reminder.Id}")
            .StartAt(reminder.RemindAtUtc.UtcDateTime)
            .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
            .Build();

        await scheduler.ScheduleJob(job, trigger, ct);

        return reminder;
    }
}
