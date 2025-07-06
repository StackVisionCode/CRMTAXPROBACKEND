
using Quartz;

namespace SharedLibrary.Quartz.Jobs;
public class ReminderJob : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        var dataMap = context.MergedJobDataMap;
        var reminderText = dataMap.GetString("Message");

        Console.WriteLine($"[Reminder] {reminderText} - {DateTime.Now}");

        // Aquí puedes enviar un correo, notificación o lo que necesites.

        return Task.CompletedTask;
    }
}
