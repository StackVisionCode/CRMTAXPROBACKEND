using Application.Common.DTO;
using Infrastructure.Commands;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;

namespace Handlers.EventHandlers.UserEventHandlers;

public sealed class AccountActivatedHandler : IIntegrationEventHandler<AccountConfirmedEvent>
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AccountActivatedHandler> _logger;

    public AccountActivatedHandler(
        IMediator mediator,
        IWebHostEnvironment env,
        ILogger<AccountActivatedHandler> logger
    )
    {
        _mediator = mediator;
        _env = env;
        _logger = logger;
    }

    public async Task Handle(AccountConfirmedEvent evt)
    {
        try
        {
            // Determinar template basado en IsCompany
            string template = evt.IsCompany
                ? "TaxUsers/CompanyActivated.html"
                : "TaxUsers/UserActivated.html";

            // Determinar nombre para mostrar con nueva lógica
            string displayName = DetermineDisplayName(evt);

            // Determinar AdminName para empresas
            string adminName = DetermineAdminName(evt);

            var model = new
            {
                DisplayName = displayName,
                Email = evt.Email,
                Name = evt.Name,
                LastName = evt.LastName,
                CompanyFullName = evt.FullName, // NUEVO: Para individuales
                CompanyName = evt.CompanyName, // NUEVO: Para empresas
                Domain = evt.Domain,
                Year = DateTime.UtcNow.Year,
                AdminName = adminName, // Para {{AdminName}} en CompanyActivated.html
                ExpirationInfo = "This activation link expires in 24 hours",
                IsCompany = evt.IsCompany,
                CompanyType = evt.IsCompany ? "Tax Firm" : "Individual Tax Preparer",
                ActivationDate = DateTime.UtcNow.ToString("MMMM dd, yyyy"),
                WelcomeMessage = evt.IsCompany
                    ? "Your tax firm is now ready to manage multiple preparers and clients"
                    : "Your professional tax preparation account is now active",
            };

            var dto = new EmailNotificationDto(
                Template: template,
                Model: model,
                Subject: evt.IsCompany
                    ? "🏢 Your Tax Firm is Now Active - TAXPRO SHIELD"
                    : "🎉 Your Account is Now Active - TAXPRO SHIELD",
                To: evt.Email,
                CompanyId: evt.CompanyId != Guid.Empty ? evt.CompanyId : null,
                UserId: evt.UserId,
                InlineLogoPath: Path.Combine(_env.ContentRootPath, "Assets", "logo.png")
            );

            await _mediator.Send(new SendEmailNotificationCommand(dto));

            _logger.LogInformation(
                "Activation email sent successfully to {Email} for {Type}",
                evt.Email,
                evt.IsCompany ? "company" : "individual"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send activation email to {Email}", evt.Email);
        }
    }

    private static string DetermineDisplayName(AccountConfirmedEvent evt)
    {
        // Para empresas: usar CompanyName
        if (evt.IsCompany && !string.IsNullOrWhiteSpace(evt.CompanyName))
        {
            return evt.CompanyName; // "Tech Solutions LLC"
        }

        // Para individuales: usar FullName
        if (!evt.IsCompany && !string.IsNullOrWhiteSpace(evt.FullName))
        {
            return evt.FullName; // "John Doe"
        }

        // Fallback: usar email
        return evt.Email;
    }

    /// <summary>
    /// Determina el nombre del administrador para mostrar en el email de empresa
    /// </summary>
    private static string DetermineAdminName(AccountConfirmedEvent evt)
    {
        // Para empresas: usar el nombre del usuario administrador
        if (evt.IsCompany)
        {
            // Opción 1: Si tenemos Name y LastName del usuario
            if (!string.IsNullOrWhiteSpace(evt.Name) && !string.IsNullOrWhiteSpace(evt.LastName))
            {
                return $"{evt.Name.Trim()} {evt.LastName.Trim()}";
            }

            // Opción 2: Solo Name
            if (!string.IsNullOrWhiteSpace(evt.Name))
            {
                return evt.Name.Trim();
            }

            // Opción 3: Usar el email como fallback
            return evt.Email;
        }

        // Para individuales: usar FullName o construir desde Name/LastName
        if (!string.IsNullOrWhiteSpace(evt.FullName))
        {
            return evt.FullName;
        }

        if (!string.IsNullOrWhiteSpace(evt.Name) && !string.IsNullOrWhiteSpace(evt.LastName))
        {
            return $"{evt.Name.Trim()} {evt.LastName.Trim()}";
        }

        return evt.Email;
    }
}
