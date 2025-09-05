using Application.Common.DTO;
using Infrastructure.Commands;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.AuthEvents;

namespace Handlers.EventHandlers.LandingEventHandlers;

public sealed class EmailConfirmationLinkHandler
    : IIntegrationEventHandler<AccountConfirmationLinkEvent>
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<EmailConfirmationLinkHandler> _logger;

    public EmailConfirmationLinkHandler(
        IMediator mediator,
        IWebHostEnvironment env,
        ILogger<EmailConfirmationLinkHandler> logger
    )
    {
        _mediator = mediator;
        _env = env;
        _logger = logger;
    }

    public async Task Handle(AccountConfirmationLinkEvent evt)
    {
        try
        {
            // Determinar template basado en IsCompany
            string template = evt.IsCompany
                ? "TaxUsers/CompanyWelcome.html"
                : "TaxUsers/UserWelcome.html";

            // Determinar nombre para mostrar con nueva lógica
            string displayName = DetermineDisplayName(evt);

            var model = new
            {
                DisplayName = displayName,
                ConfirmLink = evt.ConfirmLink,
                CompanyFullName = evt.CompanyFullName, // NUEVO: Para individuales
                CompanyName = evt.CompanyName, // NUEVO: Para empresas
                AdminName = evt.AdminName,
                Domain = evt.Domain,
                Year = DateTime.UtcNow.Year,
                // Información adicional para templates
                IsCompany = evt.IsCompany,
                CompanyType = evt.IsCompany ? "Tax Firm" : "Individual Tax Preparer",
                ExpirationInfo = $"This link expires on {evt.ExpiresAt:MMM dd, yyyy 'at' h:mm tt}",
            };

            var dto = new EmailNotificationDto(
                Template: template,
                Model: model,
                Subject: evt.IsCompany
                    ? "Welcome to TAXPRO SUITE - Confirm Your Tax Firm"
                    : "Welcome to TAXPRO SUITE - Confirm Your Account",
                To: evt.Email,
                CompanyId: evt.CompanyId != Guid.Empty ? evt.CompanyId : null,
                UserId: evt.UserId,
                InlineLogoPath: Path.Combine(_env.ContentRootPath, "Assets", "logo.png")
            );

            await _mediator.Send(new SendEmailNotificationCommand(dto));

            _logger.LogInformation(
                "Confirmation email sent successfully to {Email} for {Type}",
                evt.Email,
                evt.IsCompany ? "company" : "individual"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation email to {Email}", evt.Email);
        }
    }

    private static string DetermineDisplayName(AccountConfirmationLinkEvent evt)
    {
        // Para empresas: usar CompanyName
        if (evt.IsCompany && !string.IsNullOrWhiteSpace(evt.CompanyName))
        {
            return evt.CompanyName;
        }

        // Para individuales: usar CompanyFullName (nombre del preparador)
        if (!evt.IsCompany && !string.IsNullOrWhiteSpace(evt.CompanyFullName))
        {
            return evt.CompanyFullName;
        }

        // Fallback: usar AdminName
        if (!string.IsNullOrWhiteSpace(evt.AdminName))
        {
            return evt.AdminName;
        }

        // Último fallback: usar DisplayName del evento
        return evt.DisplayName;
    }
}
