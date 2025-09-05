using Application.Common.DTO;
using Infrastructure.Commands;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.SignatureEvents;

namespace Handlers.EventHandlers.SignatureEventHandlers;

public sealed class SignatureInvitationHandler : IIntegrationEventHandler<SignatureInvitationEvent>
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;

    public SignatureInvitationHandler(IMediator mediator, IWebHostEnvironment env)
    {
        _mediator = mediator;
        _env = env;
    }

    public Task Handle(SignatureInvitationEvent e)
    {
        var dto = new EmailNotificationDto(
            Template: "Signatures/SignatureInvitation.html",
            Model: new
            {
                SignerEmail = e.SignerEmail,
                ConfirmLink = e.ConfirmLink,
                ExpiresAt = e.ExpiresAt.ToString("dd/MM/yyyy HH:mm"),
                FullName = e.FullName,
                Year = DateTime.UtcNow.Year,
            },
            Subject: "Invitation to sign document",
            To: e.SignerEmail,
            InlineLogoPath: Path.Combine(_env.ContentRootPath, "Assets", "logo.png")
        );

        return _mediator.Send(new SendEmailNotificationCommand(dto));
    }
}
