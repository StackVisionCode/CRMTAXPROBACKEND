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
        // Join eficiente para obtener email y config validando CompanyId
        var emailWithConfig = await (
            from e in _ctx.Emails
            join c in _ctx.EmailConfigs on e.ConfigId equals c.Id
            where
                e.Id == request.EmailId
                && e.CompanyId == request.CompanyId
                && c.CompanyId == request.CompanyId
                && c.IsActive
            select new { Email = e, Config = c }
        ).FirstOrDefaultAsync(ct);

        if (emailWithConfig is null)
            throw new KeyNotFoundException("Email or configuration not found or access denied");

        var email = emailWithConfig.Email;
        var config = emailWithConfig.Config;

        if (email.Status == EmailServices.Domain.EmailStatus.Sent)
            throw new InvalidOperationException("Email already sent");

        // Actualizar auditoría antes del envío
        email.SentByTaxUserId = request.SentByTaxUserId;
        email.LastModifiedByTaxUserId = request.SentByTaxUserId;
        email.UpdatedOn = DateTime.UtcNow;

        // Envío
        await _svc.SendEmailAsync(email, config);

        // Recargar para reflejar cambios realizados por el servicio
        await _ctx.Entry(email).ReloadAsync(ct);

        _log.LogInformation(
            $"Email {request.EmailId} sent by user {request.SentByTaxUserId} from company {request.CompanyId}"
        );

        return _map.Map<EmailDTO>(email);
    }
}
