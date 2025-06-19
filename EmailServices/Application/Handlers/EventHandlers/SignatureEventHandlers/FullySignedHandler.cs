using Application.Common.DTO;
using Infrastructure.Commands;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.SignatureEvents;

namespace Handlers.EventHandlers.SignatureEventHandlers;

public sealed class FullySignedHandler : IIntegrationEventHandler<DocumentFullySignedEvent>
{
    private readonly IMediator _med;
    private readonly IWebHostEnvironment _env;

    public FullySignedHandler(IMediator m, IWebHostEnvironment e) => (_med, _env) = (m, e);

    public async Task Handle(DocumentFullySignedEvent e)
    {
        foreach (var email in e.Emails)
        {
            var dto = new EmailNotificationDto(
                Template: "Signatures/FullySigned.html",
                Model: new
                {
                    SignerEmail = email,
                    DocumentId = e.DocumentId,
                    Year = DateTime.UtcNow.Year,
                },
                Subject: "Documento completamente firmado",
                To: email,
                InlineLogoPath: Path.Combine(_env.ContentRootPath, "Assets", "logo.png")
            );
            await _med.Send(new SendEmailNotificationCommand(dto));
        }
    }
}
