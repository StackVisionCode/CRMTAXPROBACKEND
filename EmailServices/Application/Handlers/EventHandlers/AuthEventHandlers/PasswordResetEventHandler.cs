using Application.Common.DTO;
using Infrastructure.Commands;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;

namespace EmailServices.Handlers.EventsHandler;

public sealed class PasswordResetEventHandler : IIntegrationEventHandler<PasswordResetLinkEvent>
{
    private readonly IMediator _mediator;

    public PasswordResetEventHandler(IMediator mediator) => _mediator = mediator;

    public async Task Handle(PasswordResetLinkEvent e)
    {
        await _mediator.Send(
            new SendEmailNotificationCommand(
                new EmailNotificationDto(
                    Template: "Auth/PasswordResetLink.html",
                    Model: new
                    {
                        e.DisplayName,
                        e.ResetLink,
                        Exp = e.ExpiresAt.ToLocalTime(),
                    },
                    Subject: "Reset your password",
                    To: e.Email,
                    InlineLogoPath: "Assets/logo.png"
                )
            )
        );
    }
}
