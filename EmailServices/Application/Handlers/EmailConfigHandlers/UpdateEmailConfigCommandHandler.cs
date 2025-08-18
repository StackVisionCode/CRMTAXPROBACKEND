using Application.Common.DTO;
using Application.Validation;
using AutoMapper;
using Domain;
using Infrastructure.Commands;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class UpdateEmailConfigCommandHandler
    : IRequestHandler<UpdateEmailConfigCommand, EmailConfigDTO>
{
    private readonly EmailContext _ctx;
    private readonly IMapper _map;
    private readonly ILogger<UpdateEmailConfigCommandHandler> _log;
    private readonly IEmailConfigValidator _validator;

    public UpdateEmailConfigCommandHandler(
        EmailContext ctx,
        IMapper map,
        ILogger<UpdateEmailConfigCommandHandler> log,
        IEmailConfigValidator validator
    )
    {
        _ctx = ctx;
        _map = map;
        _log = log;
        _validator = validator;
    }

    public async Task<EmailConfigDTO> Handle(UpdateEmailConfigCommand r, CancellationToken ct)
    {
        var entity = await _ctx
            .EmailConfigs.Where(c => c.Id == r.Id && c.CompanyId == r.CompanyId && c.IsActive)
            .FirstOrDefaultAsync(ct);

        if (entity is null)
            throw new KeyNotFoundException("Email configuration not found or access denied");

        // Validar el DTO
        _validator.Validate(r.Config);

        // Verificar que no existe otra configuración con el mismo nombre en la compañía
        var nameExists = await _ctx
            .EmailConfigs.Where(c =>
                c.CompanyId == r.CompanyId && c.Name == r.Config.Name && c.Id != r.Id && c.IsActive
            )
            .AnyAsync(ct);

        if (nameExists)
            throw new InvalidOperationException(
                "An email configuration with this name already exists for this company"
            );

        // Preservar campos importantes antes del mapeo
        var originalId = entity.Id;
        var originalCompanyId = entity.CompanyId;
        var originalCreatedByTaxUserId = entity.CreatedByTaxUserId;
        var originalCreatedOn = entity.CreatedOn;

        // Mapear cambios
        _map.Map(r.Config, entity);

        CleanConfigFieldsByProvider(entity);

        // Restaurar campos que no deben cambiar y actualizar auditoría
        entity.Id = originalId;
        entity.CompanyId = originalCompanyId;
        entity.CreatedByTaxUserId = originalCreatedByTaxUserId;
        entity.CreatedOn = originalCreatedOn;
        entity.LastModifiedByTaxUserId = r.LastModifiedByTaxUserId;
        entity.UpdatedOn = DateTime.UtcNow;

        await _ctx.SaveChangesAsync(ct);

        _log.LogInformation(
            $"Email configuration {entity.Id} updated by user {r.LastModifiedByTaxUserId} from company {r.CompanyId}"
        );

        return _map.Map<EmailConfigDTO>(entity);
    }

    // Método helper para limpiar campos
    private void CleanConfigFieldsByProvider(EmailConfig entity)
    {
        if (entity.ProviderType?.Equals("Smtp", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Para SMTP: Limpiar todos los campos de Gmail
            entity.GmailClientId = null;
            entity.GmailClientSecret = null;
            entity.GmailRefreshToken = null;
            entity.GmailAccessToken = null;
            entity.GmailTokenExpiry = null;
            entity.GmailEmailAddress = null;
        }
        else if (entity.ProviderType?.Equals("Gmail", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Para Gmail OAuth2: Limpiar todos los campos de SMTP
            entity.SmtpServer = null;
            entity.SmtpPort = null;
            entity.EnableSsl = null;
            entity.SmtpUsername = null;
            entity.SmtpPassword = null;
        }
    }
}
