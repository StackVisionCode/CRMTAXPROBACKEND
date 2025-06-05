using EmailServices.Services.EmailNotificationsServices;
using Infrastructure.Commands;
using MediatR;

namespace EmailServices.Handlers.EmailNotificationsHandlers;

public sealed class SendEmailNotificationCommandHandler
    : IRequestHandler<SendEmailNotificationCommand, Unit>
{
    private readonly IEmailBuilder _builder;
    private readonly ISmtpSender _sender;
    private readonly IEmailConfigProvider _provider;

    public SendEmailNotificationCommandHandler(
        IEmailBuilder builder,
        ISmtpSender sender,
        IEmailConfigProvider provider
    )
    {
        _builder = builder;
        _sender = sender;
        _provider = provider;
    }

    public async Task<Unit> Handle(SendEmailNotificationCommand cmd, CancellationToken ct)
    {
        var cfg = _provider.GetConfigForEvent(cmd.Payload); // obtén SMTP adecuado
        var msg = _builder.Build(cmd.Payload, cfg); // render + adjuntos
        await _sender.SendAsync(msg, cfg, ct); // envío
        return Unit.Value;
    }
}
