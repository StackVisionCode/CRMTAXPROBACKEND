using Application.Common.DTO;
using Infrastructure.Commands;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs;

namespace EmailServices.Handlers.EventsHandler;

public sealed class PasswordResetOtpEventsHandler : IIntegrationEventHandler<PasswordResetOtpEvent>
{
    private readonly IMediator _mediator;

    public PasswordResetOtpEventsHandler(IMediator mediator) => _mediator = mediator;

    public async Task Handle(PasswordResetOtpEvent e)
    {
        await _mediator.Send(
            new SendEmailNotificationCommand(
                new EmailNotificationDto(
                    Template: "Auth/PasswordResetOtp.html",
                    Model: new
                    {
                        e.DisplayName,
                        e.Otp,
                        Exp = e.ExpiresAt.ToLocalTime(),
                    },
                    Subject: "Tu código de verificación",
                    To: e.Email,
                    InlineLogoPath: "Assets/logo.png"
                )
            )
        );
    }
}
