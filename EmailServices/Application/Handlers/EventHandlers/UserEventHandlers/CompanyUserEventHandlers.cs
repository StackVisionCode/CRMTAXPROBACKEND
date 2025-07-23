using Application.Common.DTO;
using Infrastructure.Commands;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;

namespace Handlers.EventHandlers.UserEventHandlers;

public sealed class CompanyAccountActivatedHandler
    : IIntegrationEventHandler<CompanyAccountConfirmedEvent>
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CompanyAccountActivatedHandler> _logger;

    public CompanyAccountActivatedHandler(
        IMediator mediator,
        IWebHostEnvironment environment,
        ILogger<CompanyAccountActivatedHandler> logger
    )
    {
        _mediator = mediator;
        _environment = environment;
        _logger = logger;
    }

    public async Task Handle(CompanyAccountConfirmedEvent eventData)
    {
        try
        {
            var emailDto = new EmailNotificationDto(
                Template: "CompanyUsers/CompanyUserActivated.html",
                Model: new
                {
                    DisplayName = eventData.DisplayName,
                    CompanyName = eventData.CompanyName,
                    FirstName = eventData.FirstName,
                    LastName = eventData.LastName,
                    Position = eventData.Position,
                    Year = DateTime.UtcNow.Year,
                    Email = eventData.Email,
                },
                Subject: "🏢 ¡Cuenta Empresarial Activada! - TAXPRO SHIELD",
                To: eventData.Email,
                InlineLogoPath: Path.Combine(_environment.ContentRootPath, "Assets", "logo.png")
            );

            await _mediator.Send(new SendEmailNotificationCommand(emailDto));

            _logger.LogInformation(
                "Company activation email sent successfully: UserId={UserId}, Email={Email}, Company={CompanyName}",
                eventData.UserId,
                eventData.Email,
                eventData.CompanyName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send company activation email: UserId={UserId}, Email={Email}, Error={Message}",
                eventData.UserId,
                eventData.Email,
                ex.Message
            );
            throw;
        }
    }
}
