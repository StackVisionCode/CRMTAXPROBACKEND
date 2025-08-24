using SharedLibrary.DTOs;

namespace SharedLibrary.DTOs.ReminderEvents;

public sealed record ReminderDueEvent(
    Guid Id,
    DateTime OccurredOn,
    string UserId,                 // compat con ReminderJob (UserId es string en la entidad)
    string Channel,                // "email" | "sms" | "push"
    string Subject,                // asunto sugerido
    string Message,                // cuerpo del mensaje
    string AggregateType,          // "event" | "task" | "custom"
    Guid?  AggregateId,            // id del evento/tarea
    DateTimeOffset RemindAtUtc,    // hora UTC del recordatorio
    Guid? CompanyId = null,        // opcional
    Guid? ActorUserId = null,      // opcional
    string? Email = null           // opcional: destinatario directo
) : IntegrationEvent(Id, OccurredOn);
