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
    private readonly ILogger<UserLoginEventsHandler> _logger;

    public UserLoginEventsHandler(
        IMediator mediator,
        IWebHostEnvironment env,
        ILogger<UserLoginEventsHandler> logger
    )
    {
        _mediator = mediator;
        _env = env;
        _logger = logger;
    }

    public async Task Handle(UserLoginEvent evt)
    {
        try
        {
            // Crear modelo con lógica condicional en C#
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
                evt.CompanyFullName,
                evt.CompanyName,
                evt.IsCompany,
                evt.CompanyDomain,
                evt.Year,

                // DisplayName calculado con nueva lógica
                DisplayName = DetermineDisplayName(evt),

                // Lógica condicional para el dominio
                ShowCompanyDomain = !string.IsNullOrWhiteSpace(evt.CompanyDomain),
                CompanyDomainDisplay = evt.CompanyDomain ?? "N/A",

                // Información adicional para el template
                CompanyType = evt.IsCompany ? "Tax Firm" : "Individual Tax Preparer",
                LoginTimeFormatted = evt.LoginTime.ToString("dddd, dd MMMM yyyy 'at' HH:mm"),
                CurrentYear = DateTime.Now.Year,

                // Información adicional para debugging
                DeviceInfo = FormatDeviceInfo(evt.Device),
                LocationInfo = FormatLocationInfo(evt.IpAddress),
            };

            var logoPath = Path.Combine(_env.ContentRootPath, "Assets", "logo.png");

            var dto = new EmailNotificationDto(
                Template: "Auth/Login.html",
                Model: model,
                Subject: "New login detected - TAXPRO SHIELD",
                To: evt.Email,
                CompanyId: evt.CompanyId != Guid.Empty ? evt.CompanyId : null,
                UserId: evt.UserId,
                InlineLogoPath: logoPath
            );

            await _mediator.Send(new SendEmailNotificationCommand(dto));

            _logger.LogInformation(
                "Login notification email sent successfully to {Email} for user {UserId}",
                evt.Email,
                evt.UserId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send login notification email to {Email} for user {UserId}",
                evt.Email,
                evt.UserId
            );
            // No relanzar la excepción para no afectar el proceso de login
        }
    }

    private static string DetermineDisplayName(UserLoginEvent evt)
    {
        // Para empresas (IsCompany = true): usar CompanyName
        if (evt.IsCompany && !string.IsNullOrWhiteSpace(evt.CompanyName))
        {
            return evt.CompanyName;
        }

        // Para preparadores individuales (IsCompany = false): usar CompanyFullName o nombre personal
        if (!evt.IsCompany)
        {
            // Primero intentar con CompanyFullName (nombre del preparador individual)
            if (!string.IsNullOrWhiteSpace(evt.CompanyFullName))
            {
                return evt.CompanyFullName;
            }

            // Si no hay CompanyFullName, usar Name + LastName
            if (!string.IsNullOrWhiteSpace(evt.Name) || !string.IsNullOrWhiteSpace(evt.LastName))
            {
                return $"{evt.Name} {evt.LastName}".Trim();
            }
        }

        // Fallback: usar CompanyName si existe
        if (!string.IsNullOrWhiteSpace(evt.CompanyName))
        {
            return evt.CompanyName;
        }

        // Fallback: usar CompanyFullName si existe
        if (!string.IsNullOrWhiteSpace(evt.CompanyFullName))
        {
            return evt.CompanyFullName;
        }

        // Último fallback: usar email
        return evt.Email;
    }

    /// <summary>
    /// Formatea la información del dispositivo de manera más legible
    /// </summary>
    private static string FormatDeviceInfo(string? device)
    {
        if (string.IsNullOrWhiteSpace(device))
            return "Unknown Device";

        // Mejorar el formato del user agent
        if (device.Contains("PostmanRuntime"))
            return "Postman API Client";

        if (device.Contains("Chrome"))
            return "Chrome Browser";

        if (device.Contains("Firefox"))
            return "Firefox Browser";

        if (device.Contains("Safari"))
            return "Safari Browser";

        return device.Length > 50 ? device.Substring(0, 50) + "..." : device;
    }

    /// <summary>
    /// Formatea la información de ubicación basada en IP
    /// </summary>
    private static string FormatLocationInfo(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return "Unknown Location";

        // IPs locales o de desarrollo
        if (ipAddress == "::1" || ipAddress == "127.0.0.1" || ipAddress.StartsWith("192.168."))
            return "Local Network";

        return ipAddress;
    }
}
