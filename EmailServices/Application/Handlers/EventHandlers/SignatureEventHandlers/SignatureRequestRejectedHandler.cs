using Application.Common.DTO;
using Infrastructure.Commands;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.SignatureEvents;

namespace Handlers.EventHandlers.SignatureEventHandlers;

public sealed class SignatureRequestRejectedHandler
    : IIntegrationEventHandler<SignatureRequestRejectedEvent>
{
    private readonly IMediator _mediator;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SignatureRequestRejectedHandler> _log;

    public SignatureRequestRejectedHandler(
        IMediator mediator,
        IWebHostEnvironment env,
        ILogger<SignatureRequestRejectedHandler> log
    )
    {
        _mediator = mediator;
        _env = env;
        _log = log;
    }

    public Task Handle(SignatureRequestRejectedEvent e)
    {
        string rejectingDisplay = !string.IsNullOrWhiteSpace(e.RejectedByFullName)
            ? e.RejectedByFullName
            : e.RejectedByEmail;

        string recipientDisplay = !string.IsNullOrWhiteSpace(e.RecipientFullName)
            ? e.RecipientFullName
            : e.RecipientEmail;

        var dto = new EmailNotificationDto(
            Template: "Signatures/SignatureRejected.html",
            Model: new
            {
                RecipientName = recipientDisplay,
                RejectingName = rejectingDisplay,
                RejectedAt = e.RejectedAtUtc.ToString("yyyy-MM-dd HH:mm 'UTC'"),
                Reason = string.IsNullOrWhiteSpace(e.Reason)
                    ? "No specific reason was provided."
                    : e.Reason,
                RequestId = e.SignatureRequestId,
                RequestIdShort = e.SignatureRequestId.ToString().Substring(0, 8),
                Year = DateTime.UtcNow.Year,
            },
            Subject: "Digital Signature Request Rejected – Action Required",
            To: e.RecipientEmail,
            InlineLogoPath: Path.Combine(_env.ContentRootPath, "Assets", "logo.png")
        );

        return _mediator.Send(new SendEmailNotificationCommand(dto));
    }
}
