using Application.Common.DTO;
using Infrastructure.Commands;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.InvitationEvents;

namespace Handlers.EventHandlers.InvitationEventHandlers;

public sealed class UserInvitationSentHandler : IIntegrationEventHandler<UserInvitationSentEvent>
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<UserInvitationSentHandler> _logger;

    public UserInvitationSentHandler(
        IMediator mediator,
        IWebHostEnvironment env,
        ILogger<UserInvitationSentHandler> logger
    )
    {
        _mediator = mediator;
        _env = env;
        _logger = logger;
    }

    public async Task Handle(UserInvitationSentEvent evt)
    {
        try
        {
            string template = "Invitations/TeamInvitation.html";
            string companyDisplayName = DetermineCompanyDisplayName(evt);

            // Crear sección de mensaje personal si existe
            string personalMessageSection = "";
            if (!string.IsNullOrWhiteSpace(evt.PersonalMessage))
            {
                personalMessageSection =
                    $@"
                <div style=""background: rgba(16, 185, 129, 0.1); border-radius: 8px; padding: 15px; margin: 20px 0;"">
                  <p style=""margin: 0; color: #166534; font-size: 14px; font-style: italic;"">
                    <strong>Personal Message:</strong><br>
                    ""{evt.PersonalMessage}""
                  </p>
                </div>";
            }

            var model = new
            {
                Email = evt.Email,
                CompanyDisplayName = companyDisplayName,
                CompanyName = evt.CompanyName,
                CompanyFullName = evt.CompanyFullName,
                CompanyDomain = evt.CompanyDomain,
                InvitationLink = evt.InvitationLink,
                ExpiresAt = evt.ExpiresAt,
                PersonalMessage = evt.PersonalMessage ?? "",
                PersonalMessageSection = personalMessageSection,
                IsCompany = evt.IsCompany,
                CompanyType = evt.IsCompany ? "Tax Firm" : "Individual Tax Preparer",
                ExpirationInfo = $"This invitation expires on {evt.ExpiresAt:MMM dd, yyyy 'at' h:mm tt}",
                Year = DateTime.UtcNow.Year,
            };

            var dto = new EmailNotificationDto(
                Template: template,
                Model: model,
                Subject: $"You're invited to join {companyDisplayName} - TAXPRO SHIELD",
                To: evt.Email,
                CompanyId: evt.CompanyId != Guid.Empty ? evt.CompanyId : null,
                UserId: null,
                InlineLogoPath: Path.Combine(_env.ContentRootPath, "Assets", "logo.png")
            );

            await _mediator.Send(new SendEmailNotificationCommand(dto));

            _logger.LogInformation(
                "Invitation email sent successfully to {Email} for company {CompanyId}",
                evt.Email,
                evt.CompanyId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invitation email to {Email}", evt.Email);
        }
    }

    private static string DetermineCompanyDisplayName(UserInvitationSentEvent evt)
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
