using Application.Common.DTO;
using Infrastructure.Commands;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.InvitationEvents;

namespace Handlers.EventHandlers.InvitationEventHandlers;

public sealed class UserCompanyRegisteredHandler
    : IIntegrationEventHandler<UserCompanyRegisteredEvent>
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<UserCompanyRegisteredHandler> _logger;

    public UserCompanyRegisteredHandler(
        IMediator mediator,
        IWebHostEnvironment env,
        ILogger<UserCompanyRegisteredHandler> logger
    )
    {
        _mediator = mediator;
        _env = env;
        _logger = logger;
    }

    public async Task Handle(UserCompanyRegisteredEvent evt)
    {
        try
        {
            string template = "Invitations/WelcomeNewTeamMember.html";

            string employeeName = DetermineEmployeeName(evt);
            string companyDisplayName = DetermineCompanyDisplayName(evt);

            var model = new
            {
                EmployeeName = employeeName,
                Email = evt.Email,
                CompanyDisplayName = companyDisplayName,
                CompanyName = evt.CompanyName,
                CompanyFullName = evt.CompanyFullName,
                CompanyDomain = evt.CompanyDomain,
                IsCompany = evt.IsCompany,
                CompanyType = evt.IsCompany ? "Tax Firm" : "Individual Tax Preparer",
                JoinDate = DateTime.UtcNow.ToString("MMMM dd, yyyy"),
                Year = DateTime.UtcNow.Year,
            };

            var dto = new EmailNotificationDto(
                Template: template,
                Model: model,
                Subject: $"Welcome to {companyDisplayName} Team - TAXPRO SHIELD",
                To: evt.Email,
                CompanyId: evt.CompanyId != Guid.Empty ? evt.CompanyId : null,
                UserId: evt.UserCompanyId,
                InlineLogoPath: Path.Combine(_env.ContentRootPath, "Assets", "logo.png")
            );

            await _mediator.Send(new SendEmailNotificationCommand(dto));

            _logger.LogInformation(
                "Welcome email sent successfully to {Email} for company {CompanyId}",
                evt.Email,
                evt.CompanyId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", evt.Email);
        }
    }

    private static string DetermineEmployeeName(UserCompanyRegisteredEvent evt)
    {
        var nameParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(evt.Name))
            nameParts.Add(evt.Name);

        if (!string.IsNullOrWhiteSpace(evt.LastName))
            nameParts.Add(evt.LastName);

        return nameParts.Any() ? string.Join(" ", nameParts) : evt.Email;
    }

    private static string DetermineCompanyDisplayName(UserCompanyRegisteredEvent evt)
    {
        if (evt.IsCompany && !string.IsNullOrWhiteSpace(evt.CompanyName))
        {
            return evt.CompanyName;
        }

        if (!evt.IsCompany && !string.IsNullOrWhiteSpace(evt.CompanyFullName))
        {
            return evt.CompanyFullName;
        }

        return evt.CompanyDomain ?? "the organization";
    }
}
