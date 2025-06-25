using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Application.Common.DTO;
using EmailServices.Services;
using Infrastructure.Commands;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;

namespace EmailServices.Handlers.EventsHandler;

public sealed class UserLoginEventsHandler : IIntegrationEventHandler<UserLoginEvent>
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;

    public UserLoginEventsHandler(IMediator mediator, IWebHostEnvironment env)
    {
        _mediator = mediator;
        _env = env;
    }

    public Task Handle(UserLoginEvent evt)
    {
        // 1. Crear un objeto expandido con FullName calculado
        var model = new
        {
            evt.Id,
            evt.OccurredOn,
            evt.UserId,
            evt.Email,
            evt.Name,
            evt.LastName,
            evt.LoginTime,
            evt.IpAddress,
            evt.Device,
            evt.CompanyId,
            evt.CompanyName,
            evt.FullName,
            // Lógica para determinar FullName
            DisplayName = DetermineDisplayName(evt),
            Year = DateTime.Now.Year,
        };

        var logoPath = Path.Combine(_env.ContentRootPath, "Assets", "logo.png");

        var dto = new EmailNotificationDto(
            Template: "Auth/Login.html",
            Model: model,
            Subject: "Nuevo inicio de sesión detectado",
            To: evt.Email,
            CompanyId: null,
            UserId: null,
            InlineLogoPath: logoPath
        );

        return _mediator.Send(new SendEmailNotificationCommand(dto));
    }

    private static string DetermineDisplayName(UserLoginEvent evt)
    {
        // Si tiene CompanyName y no tiene Name/LastName (usuario de oficina)
        if (
            !string.IsNullOrWhiteSpace(evt.CompanyName)
            && string.IsNullOrWhiteSpace(evt.Name)
            && string.IsNullOrWhiteSpace(evt.LastName)
        )
        {
            return evt.CompanyName;
        }

        // Si tiene Name o LastName (usuario individual)
        if (!string.IsNullOrWhiteSpace(evt.Name) || !string.IsNullOrWhiteSpace(evt.LastName))
        {
            return $"{evt.Name} {evt.LastName}".Trim();
        }

        // Fallback: usar CompanyName si existe
        if (!string.IsNullOrWhiteSpace(evt.CompanyName))
        {
            return evt.CompanyName;
        }

        // Último fallback: usar email
        return evt.Email;
    }
}
