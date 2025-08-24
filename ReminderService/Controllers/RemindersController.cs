using Application.DTO;
using Domain.Entities;
using Infrastructure.Context;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Controllers;

[ApiController]
[Route("api/reminders")]
public class RemindersController : ControllerBase
{
    private readonly ReminderDbContext _db;
    private readonly ReminderScheduler _scheduler;

    public RemindersController(ReminderDbContext db, ReminderScheduler scheduler)
    {
        _db = db; _scheduler = scheduler;
    }

    // CalendarService → programar recordatorios por días antes del EVENTO
    [HttpPost("events/{eventId:guid}")]
    public async Task<IActionResult> ScheduleForEvent(Guid eventId, [FromBody] ScheduleEventReminderRequest body)
    {
        var baseDate = body.EventStartUtc.UtcDateTime.Date;
        var time = body.RemindAtTime ?? TimeSpan.FromHours(9); // 09:00 por defecto

        foreach (var d in body.DaysBefore.Distinct().Where(x => x >= 0).OrderByDescending(x => x))
        {
            var remindAt = baseDate.AddDays(-d).Add(time);
            await _scheduler.ScheduleOneShotAsync(new Reminder
            {
                AggregateType = "event",
                AggregateId = eventId,
                UserId = body.UserId ?? "unknown",
                Channel = body.Channel,
                Message = body.Message ?? "Tienes una reunión próximamente",
                RemindAtUtc = DateTime.SpecifyKind(remindAt, DateTimeKind.Utc)
            });
        }
        return Ok();
    }

    // CalendarService → programar recordatorios por días antes de la TAREA
    [HttpPost("tasks/{taskId:guid}")]
    public async Task<IActionResult> ScheduleForTask(Guid taskId, [FromBody] ScheduleTaskReminderRequest body)
    {
        var baseDate = body.DueAtUtc.UtcDateTime.Date;
        var time = body.RemindAtTime ?? TimeSpan.FromHours(9);

        foreach (var d in body.DaysBefore.Distinct().Where(x => x >= 0).OrderByDescending(x => x))
        {
            var remindAt = baseDate.AddDays(-d).Add(time);
            await _scheduler.ScheduleOneShotAsync(new Reminder
            {
                AggregateType = "task",
                AggregateId = taskId,
                UserId = body.UserId ?? "unknown",
                Channel = body.Channel,
                Message = body.Message ?? "Tienes una tarea pendiente",
                RemindAtUtc = DateTime.SpecifyKind(remindAt, DateTimeKind.Utc)
            });
        }
        return Ok();
    }

    // Programar un recordatorio exacto (libre)
    [HttpPost]
    public async Task<ActionResult<Guid>> ScheduleExact([FromBody] ScheduleExactReminderRequest body)
    {
        var reminder = await _scheduler.ScheduleOneShotAsync(new Reminder
        {
            AggregateType = body.AggregateType,
            AggregateId = body.AggregateId,
            UserId = body.UserId ?? "unknown",
            Channel = body.Channel,
            Message = body.Message ?? "Recordatorio",
            RemindAtUtc = body.RemindAtUtc.ToUniversalTime()
        });
        return Ok(reminder.Id);
    }

    // Cancelar (soft delete)
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var r = await _db.Reminders.FirstOrDefaultAsync(x => x.Id == id);
        if (r is null) return NotFound();
        r.Status = "cancelled";
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Listar próximos (ej: para UI)
    [HttpGet("upcoming")]
    public async Task<ActionResult<IEnumerable<Reminder>>> Upcoming([FromQuery] int hours = 24)
    {
        var now = DateTimeOffset.UtcNow;
        var to = now.AddHours(hours);
        var list = await _db.Reminders
            .AsNoTracking()
            .Where(r => r.RemindAtUtc >= now && r.RemindAtUtc <= to && r.Status == "scheduled")
            .OrderBy(r => r.RemindAtUtc)
            .ToListAsync();

        return Ok(list);
    }
}
