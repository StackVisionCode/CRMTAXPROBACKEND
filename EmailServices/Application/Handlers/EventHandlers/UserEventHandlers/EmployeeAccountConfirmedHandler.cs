using Application.Common.DTO;
using Infrastructure.Commands;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.AuthEvents;

namespace Handlers.EventHandlers.UserEventHandlers;

public sealed class EmployeeAccountConfirmedHandler
    : IIntegrationEventHandler<EmployeeAccountConfirmedEvent>
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<EmployeeAccountConfirmedHandler> _logger;

    public EmployeeAccountConfirmedHandler(
        IMediator mediator,
        IWebHostEnvironment env,
        ILogger<EmployeeAccountConfirmedHandler> logger
    )
    {
        _mediator = mediator;
        _env = env;
        _logger = logger;
    }

    public async Task Handle(EmployeeAccountConfirmedEvent evt)
    {
        try
        {
            // Template específico para empleados activados
            string template = "TaxUsers/EmployeeActivated.html";

            // Determinar nombre del empleado
            string employeeName = DetermineEmployeeName(evt);

            // Determinar nombre de la company
            string companyDisplayName = DetermineCompanyDisplayName(evt);

            var model = new
            {
                EmployeeName = employeeName,
                Email = evt.Email,
                CompanyDisplayName = companyDisplayName,
                CompanyFullName = evt.CompanyFullName,
                CompanyName = evt.CompanyName,
                CompanyDomain = evt.CompanyDomain,
                IsCompany = evt.IsCompany,
                CompanyBrand = evt.CompanyBrand,
                Roles = string.Join(", ", evt.Roles),
                Year = DateTime.UtcNow.Year,
                CompanyType = evt.IsCompany ? "Tax Firm" : "Individual Tax Preparer",
                ActivationDate = DateTime.UtcNow.ToString("MMMM dd, yyyy"),
                WelcomeMessage = $"Your account with {companyDisplayName} is now active and ready to use",
                // Para compatibilidad con template
                DisplayName = employeeName,
            };

            var dto = new EmailNotificationDto(
                Template: template,
                Model: model,
                Subject: $"🎉 Account Activated - Welcome to {companyDisplayName} Team",
                To: evt.Email,
                CompanyId: evt.CompanyId != Guid.Empty ? evt.CompanyId : null,
                UserId: evt.UserId,
                InlineLogoPath: Path.Combine(_env.ContentRootPath, "Assets", "logo.png")
            );

            await _mediator.Send(new SendEmailNotificationCommand(dto));

            _logger.LogInformation(
                "Employee activation email sent successfully to {Email} for company {CompanyId}",
                evt.Email,
                evt.CompanyId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send employee activation email to {Email}", evt.Email);
        }
    }

    private static string DetermineEmployeeName(EmployeeAccountConfirmedEvent evt)
    {
        var nameParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(evt.Name))
            nameParts.Add(evt.Name);

        if (!string.IsNullOrWhiteSpace(evt.LastName))
            nameParts.Add(evt.LastName);

        return nameParts.Any() ? string.Join(" ", nameParts) : evt.Email;
    }

    private static string DetermineCompanyDisplayName(EmployeeAccountConfirmedEvent evt)
    {
        // Para empresas: usar CompanyName
        if (evt.IsCompany && !string.IsNullOrWhiteSpace(evt.CompanyName))
        {
            return evt.CompanyName;
        }

        // Para individuales: usar CompanyFullName (nombre del dueño)
        if (!evt.IsCompany && !string.IsNullOrWhiteSpace(evt.CompanyFullName))
        {
            return evt.CompanyFullName;
        }

        // Fallback: usar domain
        return evt.CompanyDomain ?? "the organization";
    }
}
