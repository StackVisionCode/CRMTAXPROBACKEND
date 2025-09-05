using Application.Common.DTO;
using Infrastructure.Commands;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.CustomerEventsDTO;

namespace EmailServices.Handlers.EventsHandler;

public sealed class CustomerLoginEnabledEventHandler
    : IIntegrationEventHandler<CustomerLoginEnabledEvent>
{
    private readonly IMediator _med;
    private readonly IWebHostEnvironment _env;

    public CustomerLoginEnabledEventHandler(IMediator med, IWebHostEnvironment env)
    {
        _med = med;
        _env = env;
    }

    public Task Handle(CustomerLoginEnabledEvent e)
    {
        var template = e.TempPassword is null
            ? "Customers/CustomerReenabled.html"
            : "Customers/CustomerWelcome.html";

        var dto = new EmailNotificationDto(
            Template: template,
            Model: new
            {
                DisplayName = e.DisplayName,
                Email = e.Email,
                TempPassword = e.TempPassword,
                Year = DateTime.UtcNow.Year,
            },
            Subject: "Access enabled to your portal",
            To: e.Email,
            InlineLogoPath: Path.Combine(_env.ContentRootPath, "Assets", "logo.png")
        );
        return _med.Send(new SendEmailNotificationCommand(dto));
    }
}
