using Application.Common.DTO;
using Infrastructure.Commands;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.CustomerEventsDTO;

namespace EmailServices.Handlers.EventsHandler;

public sealed class CustomerLoginDisabledEventHandler
    : IIntegrationEventHandler<CustomerLoginDisabledEvent>
{
    private readonly IMediator _med;
    private readonly IWebHostEnvironment _env;

    public CustomerLoginDisabledEventHandler(IMediator med, IWebHostEnvironment env)
    {
        _med = med;
        _env = env;
    }

    public Task Handle(CustomerLoginDisabledEvent e)
    {
        var dto = new EmailNotificationDto(
            Template: "Customers/CustomerDisabled.html",
            Model: new
            {
                DisplayName = e.DisplayName,
                Email = e.Email,
                Year = DateTime.UtcNow.Year,
            },
            Subject: "Access to your portal is disabled",
            To: e.Email,
            InlineLogoPath: Path.Combine(_env.ContentRootPath, "Assets", "logo.png")
        );
        return _med.Send(new SendEmailNotificationCommand(dto));
    }
}
