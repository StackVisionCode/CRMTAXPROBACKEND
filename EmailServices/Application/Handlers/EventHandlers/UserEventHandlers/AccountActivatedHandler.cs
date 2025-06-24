using Application.Common.DTO;
using Infrastructure.Commands;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;

namespace Handlers.EventHandlers.UserEventHandlers;

public sealed class AccountActivatedHandler : IIntegrationEventHandler<AccountConfirmedEvent>
{
    private readonly IMediator _med;
    private readonly IWebHostEnvironment _env;

    public AccountActivatedHandler(IMediator m, IWebHostEnvironment e)
    {
        _med = m;
        _env = e;
    }

    public Task Handle(AccountConfirmedEvent e)
    {
        string tpl = e.IsCompany ? "TaxUsers/CompanyActivated.html" : "TaxUsers/UserActivated.html";

        var dto = new EmailNotificationDto(
            Template: tpl,
            Model: new { DisplayName = e.DisplayName, Year = DateTime.UtcNow.Year },
            Subject: "Tu cuenta ha sido activada ✔",
            To: e.Email,
            InlineLogoPath: Path.Combine(_env.ContentRootPath, "Assets", "logo.png")
        );

        return _med.Send(new SendEmailNotificationCommand(dto));
    }
}
