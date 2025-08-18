using Application.Common.DTO;
using Application.Validation;
using AutoMapper;
using Domain;
using Infrastructure.Commands;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class CreateEmailConfigCommandHandler
    : IRequestHandler<CreateEmailConfigCommand, EmailConfigDTO>
{
    private readonly EmailContext _ctx;
    private readonly IMapper _map;
    private readonly ILogger<CreateEmailConfigCommandHandler> _log;
    private readonly IEmailConfigValidator _validator;

    public CreateEmailConfigCommandHandler(
        EmailContext ctx,
        IMapper map,
        ILogger<CreateEmailConfigCommandHandler> log,
        IEmailConfigValidator validator
    )
    {
        _ctx = ctx;
        _map = map;
        _log = log;
        _validator = validator;
    }

    public async Task<EmailConfigDTO> Handle(CreateEmailConfigCommand r, CancellationToken ct)
    {
        // Validar el DTO
        _validator.Validate(r.Config);

        // Verificar que no existe otra configuración con el mismo nombre en la compañía
        var nameExists = await _ctx
            .EmailConfigs.Where(c =>
                c.CompanyId == r.CompanyId && c.Name == r.Config.Name && c.IsActive
            )
            .AnyAsync(ct);

        if (nameExists)
            throw new InvalidOperationException(
                "An email configuration with this name already exists for this company"
            );

        var entity = _map.Map<EmailConfig>(r.Config);

        CleanConfigFieldsByProvider(entity);

        // Establecer auditoría y campos obligatorios
        entity.Id = Guid.NewGuid();
        entity.CompanyId = r.CompanyId;
        entity.CreatedByTaxUserId = r.CreatedByTaxUserId;
        entity.CreatedOn = DateTime.UtcNow;
        entity.IsActive = true;

        _ctx.EmailConfigs.Add(entity);
        await _ctx.SaveChangesAsync(ct);

        _log.LogInformation(
            $"Email configuration {entity.Id} created by user {r.CreatedByTaxUserId} for company {r.CompanyId}"
        );

        return _map.Map<EmailConfigDTO>(entity);
    }

    // Método helper para limpiar campos
    private void CleanConfigFieldsByProvider(EmailConfig entity)
    {
        if (entity.ProviderType?.Equals("Smtp", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Limpiar todos los campos de Gmail
            entity.GmailClientId = null;
            entity.GmailClientSecret = null;
            entity.GmailRefreshToken = null;
            entity.GmailAccessToken = null;
            entity.GmailTokenExpiry = null;
            entity.GmailEmailAddress = null;

            _log.LogDebug("Cleaned Gmail fields for SMTP configuration");
        }
        else if (entity.ProviderType?.Equals("Gmail", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Limpiar todos los campos de SMTP
            entity.SmtpServer = null;
            entity.SmtpPort = null;
            entity.EnableSsl = null;
            entity.SmtpUsername = null;
            entity.SmtpPassword = null;

            _log.LogDebug("Cleaned SMTP fields for Gmail configuration");
        }
    }
}
