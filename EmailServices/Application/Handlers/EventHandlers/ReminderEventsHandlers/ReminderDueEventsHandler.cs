using Application.Common.DTO;
using Infrastructure.Commands;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.ReminderEvents;
namespace Handlers.EventHandlers.ReminderEventsHandlers;

public sealed class ReminderDueEventsHandler : IIntegrationEventHandler<ReminderDueEvent>
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ReminderDueEventsHandler> _logger;

    public ReminderDueEventsHandler(
        IMediator mediator,
        IWebHostEnvironment env,
        ILogger<ReminderDueEventsHandler> logger
    )
    {
        _mediator = mediator;
        _env = env;
        _logger = logger;
    }

    public async Task Handle(ReminderDueEvent evt)
    {
        try
        {
            // Modelo para el template
            var model = new
            {
                evt.Subject,
                evt.Message,
                evt.AggregateType,
                evt.AggregateId,
                RemindAtLocal = TimeZoneInfo.ConvertTimeFromUtc(
                    evt.RemindAtUtc.UtcDateTime,
                    TimeZoneInfo.FindSystemTimeZoneById("America/Santo_Domingo")
                ).ToString("dddd, dd MMMM yyyy 'a las' HH:mm"),
                OccurredOn = evt.OccurredOn.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"),
            };

            var logoPath = Path.Combine(_env.ContentRootPath, "Assets", "logo.png");
            Guid? userIdGuid = null;
            if (!string.IsNullOrWhiteSpace(evt.UserId) && Guid.TryParse(evt.UserId, out var parsed))
            {
                userIdGuid = parsed;
            }
            var dto = new EmailNotificationDto(
                Template: "Reminders/Due.html", // <- crea este template debajo
                Model: model,
                Subject: string.IsNullOrWhiteSpace(evt.Subject) ? "Recordatorio" : evt.Subject,
                To: evt.Email!, // si prefieres resolver por UserId, cámbialo aquí
                CompanyId: evt.CompanyId,
                UserId: userIdGuid,
                InlineLogoPath: logoPath
            );

            await _mediator.Send(new SendEmailNotificationCommand(dto));

            _logger.LogInformation("Reminder email sent to {Email} (User {UserId})", evt.Email, evt.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reminder email to {Email} (User {UserId})", evt.Email, evt.UserId);
            // No relanzar para no interrumpir el pipeline de eventos
        }
    }
}