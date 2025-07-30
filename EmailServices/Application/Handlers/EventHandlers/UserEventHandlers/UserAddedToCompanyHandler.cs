using Application.Common.DTO;
using Infrastructure.Commands;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.AuthEvents;

namespace Handlers.EventHandlers.UserEventHandlers;

/// <summary>
/// Handler para cuando se agrega un usuario empleado a una company
/// Este es diferente a AccountConfirmationLinkHandler que maneja administradores
/// </summary>
public sealed class UserAddedToCompanyHandler : IIntegrationEventHandler<UserAddedToCompanyEvent>
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<UserAddedToCompanyHandler> _logger;

    public UserAddedToCompanyHandler(
        IMediator mediator,
        IWebHostEnvironment env,
        ILogger<UserAddedToCompanyHandler> logger
    )
    {
        _mediator = mediator;
        _env = env;
        _logger = logger;
    }

    public async Task Handle(UserAddedToCompanyEvent evt)
    {
        try
        {
            // Para usuarios empleados, siempre usamos el template de empleado
            string template = "TaxUsers/EmployeeWelcome.html";

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
                Roles = evt.Roles,
                Year = DateTime.UtcNow.Year,
                CompanyType = evt.IsCompany ? "Tax Firm" : "Individual Tax Preparer",
                WelcomeMessage = $"You've been added as a team member to {companyDisplayName}",
                // Para el template
                DisplayName = employeeName,
            };

            var dto = new EmailNotificationDto(
                Template: template,
                Model: model,
                Subject: $"Welcome to {companyDisplayName} - TAXPRO SHIELD Team",
                To: evt.Email,
                CompanyId: evt.CompanyId != Guid.Empty ? evt.CompanyId : null,
                UserId: evt.UserId,
                InlineLogoPath: Path.Combine(_env.ContentRootPath, "Assets", "logo.png")
            );

            await _mediator.Send(new SendEmailNotificationCommand(dto));

            _logger.LogInformation(
                "Employee welcome email sent successfully to {Email} for company {CompanyId}",
                evt.Email,
                evt.CompanyId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send employee welcome email to {Email}", evt.Email);
        }
    }

    private static string DetermineEmployeeName(UserAddedToCompanyEvent evt)
    {
        // Construir nombre del empleado
        var nameParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(evt.Name))
            nameParts.Add(evt.Name);

        if (!string.IsNullOrWhiteSpace(evt.LastName))
            nameParts.Add(evt.LastName);

        return nameParts.Any() ? string.Join(" ", nameParts) : evt.Email;
    }

    private static string DetermineCompanyDisplayName(UserAddedToCompanyEvent evt)
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
        return evt.CompanyDomain ?? "the company";
    }
}
