using Domain.Entities;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.ReminderEvents;

namespace Jobs;

public class ReminderJob(
    ReminderDbContext db,
    IEventBus eventBus,
    ILogger<ReminderJob> log
) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;

        // Id del recordatorio inyectado en el trigger
        var idStr = context.MergedJobDataMap.GetString("reminderId");
        if (!Guid.TryParse(idStr, out var id))
        {
            log.LogWarning("ReminderJob sin reminderId válido en JobDataMap.");
            return;
        }

        // Cargar el reminder
        var r = await db.Reminders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null)
        {
            log.LogWarning("Reminder {Id} no encontrado; abortando job.", id);
            return;
        }

        // Evitar envíos duplicados
        if (r.Status is "cancelled" or "sent")
        {
            log.LogInformation("Reminder {Id} con estado {Status}; no se enviará.", r.Id, r.Status);
            return;
        }

        try
        {
            // Validaciones/fallbacks mínimos
            var safeMessage = string.IsNullOrWhiteSpace(r.Message)
                ? "Tienes un recordatorio pendiente."
                : r.Message;

            var subject = "[TaxPro] Recordatorio";

            // Crear evento de integración para el bus compartido
            var evt = new ReminderDueEvent(
                Id: Guid.NewGuid(),
                OccurredOn: DateTime.UtcNow,
                UserId: r.UserId,                 // tu entidad Reminder tiene UserId como string: el evento también
                Channel: r.Channel,               // "email" | "sms" | "push"
                Subject: subject,
                Message: safeMessage,
                AggregateType: r.AggregateType,   // "event" | "task" | "custom"
                AggregateId: r.AggregateId,
                RemindAtUtc: r.RemindAtUtc        // Debe venir ya en UTC desde el programado
                // CompanyId / ActorUserId opcionales si los agregas al modelo
            );

            // Publicar (tu EventBus encola si no hay conexión y reintenta)
            eventBus.Publish(evt);

            // Marcar como enviado
            r.Status = "sent";
            await db.SaveChangesAsync(ct);

            log.LogInformation(
                "Reminder {Id} publicado a EventBus a las {Utc} (AggType={AggType}, AggId={AggId})",
                r.Id, DateTimeOffset.UtcNow, r.AggregateType, r.AggregateId
            );
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error publicando Reminder {Id} al EventBus", r.Id);
            r.Status = "failed";
            await db.SaveChangesAsync(ct);
            throw; // dejar que Quartz registre el fallo/misfire policy
        }
    }
}
