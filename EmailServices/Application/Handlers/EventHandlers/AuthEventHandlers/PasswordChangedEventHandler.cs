using Application.Common.DTO;
using Infrastructure.Commands;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;

namespace AuthService.Handlers.PasswordEventsHandler;

public class PasswordChangedEventHandler : IIntegrationEventHandler<PasswordChangedEvent>
{
    private readonly IMediator _med;
    private readonly IWebHostEnvironment _env;

    public PasswordChangedEventHandler(IMediator m, IWebHostEnvironment env) =>
        (_med, _env) = (m, env);

    public Task Handle(PasswordChangedEvent e)
    {
        var dto = new EmailNotificationDto(
            Template: "Auth/PasswordChanged.html",
            Model: new { e.DisplayName, e.ChangedAt },
            Subject: "Contraseña actualizada correctamente",
            To: e.Email,
            InlineLogoPath: Path.Combine(_env.ContentRootPath, "Assets", "logo.png")
        );

        return _med.Send(new SendEmailNotificationCommand(dto));
    }
}
