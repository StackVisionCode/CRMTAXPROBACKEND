using Application.Common.DTO;
using AutoMapper;
using Infrastructure.Commands;
using Infrastructure.Context;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class SendEmailHandler : IRequestHandler<SendEmailCommand, EmailDTO>
{
    private readonly EmailContext _ctx;
    private readonly IEmailService _svc;
    private readonly IMapper _map;
    private readonly ILogger<SendEmailHandler> _log;

    public SendEmailHandler(
        EmailContext ctx,
        IEmailService svc,
        IMapper map,
        ILogger<SendEmailHandler> log
    )
    {
        _ctx = ctx;
        _svc = svc;
        _map = map;
        _log = log;
    }

    public async Task<EmailDTO> Handle(SendEmailCommand request, CancellationToken ct)
    {
        var email = await _ctx.Emails.FirstOrDefaultAsync(e => e.Id == request.EmailId, ct);
        if (email is null)
            throw new KeyNotFoundException("Email not found");

        if (email.Status == EmailServices.Domain.EmailStatus.Sent)
            throw new InvalidOperationException("Email already sent");

        var cfg = await _ctx.EmailConfigs.FindAsync(new object[] { email.ConfigId }, ct);
        if (cfg is null)
            throw new InvalidOperationException("Configuration missing");

        // envío
        await _svc.SendEmailAsync(email, cfg);

        // recargamos para reflejar cambios realizados por el servicio
        await _ctx.Entry(email).ReloadAsync(ct);

        return _map.Map<EmailDTO>(email);
    }
}
