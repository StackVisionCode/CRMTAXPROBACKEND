using Application.Common.DTO;
using Infrastructure.Commands;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.AuthEvents;

public sealed class AccountConfirmationLinkHandler
    : IIntegrationEventHandler<AccountConfirmationLinkEvent>
{
    private readonly IMediator _med;
    private readonly IWebHostEnvironment _env;

    public AccountConfirmationLinkHandler(IMediator m, IWebHostEnvironment e)
    {
        _med = m;
        _env = e;
    }

    public Task Handle(AccountConfirmationLinkEvent e)
    {
        string tpl = e.IsCompany ? "TaxUsers/CompanyWelcome.html" : "TaxUsers/UserWelcome.html";

        var dto = new EmailNotificationDto(
            Template: tpl,
            Model: new
            {
                DisplayName = e.DisplayName,
                ConfirmLink = e.ConfirmLink,
                CompanyName = e.CompanyName,
                AdminName = e.AdminName,
                Domain = e.Domain,
                Year = DateTime.UtcNow.Year,
            },
            Subject: "Confirma tu cuenta en TaxCloud",
            To: e.Email,
            InlineLogoPath: Path.Combine(_env.ContentRootPath, "Assets", "logo.png")
        );

        return _med.Send(new SendEmailNotificationCommand(dto));
    }
}
