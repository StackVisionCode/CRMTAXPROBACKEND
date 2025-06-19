using Application.Common.DTO;
using Infrastructure.Commands;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.SignatureEvents;

namespace Handlers.EventHandlers.SignatureEventHandlers;

public sealed class PartiallySignedHandler : IIntegrationEventHandler<DocumentPartiallySignedEvent>
{
    private readonly IMediator _med;
    private readonly IWebHostEnvironment _env;

    public PartiallySignedHandler(IMediator m, IWebHostEnvironment e) => (_med, _env) = (m, e);

    public Task Handle(DocumentPartiallySignedEvent e)
    {
        var dto = new EmailNotificationDto(
            Template: "Signatures/PartiallySigned.html",
            Model: new
            {
                SignerEmail = e.SignerEmail,
                DocumentId = e.DocumentId,
                Year = DateTime.UtcNow.Year,
            },
            Subject: "Tu firma ha sido registrada",
            To: e.SignerEmail,
            InlineLogoPath: Path.Combine(_env.ContentRootPath, "Assets", "logo.png")
        );
        return _med.Send(new SendEmailNotificationCommand(dto));
    }
}
