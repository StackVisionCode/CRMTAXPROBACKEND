using Quartz;
using SharedLibrary.Quartz.Jobs;
using SharedLibrary.Quartz.Models;

namespace SharedLibrary.Quartz.Schedulers;

public class QuartzScheduler
{
    private readonly ISchedulerFactory _schedulerFactory;

    public QuartzScheduler(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    public async Task ScheduleReminder(ReminderJobData data)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        await scheduler.Start();

        var job = JobBuilder.Create<ReminderJob>()
            .WithIdentity(Guid.NewGuid().ToString())
            .UsingJobData("Message", data.Message)
            .Build();

        var trigger = TriggerBuilder.Create()
            .StartAt(data.ExecuteAt)
            .WithSimpleSchedule(x => x.WithMisfireHandlingInstructionFireNow())
            .Build();

        await scheduler.ScheduleJob(job, trigger);
    }
}
