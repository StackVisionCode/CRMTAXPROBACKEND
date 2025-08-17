using Application.Common.DTO;
using AutoMapper;
using Infrastructure.Commands;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class UpdateEmailHandler : IRequestHandler<UpdateEmailCommand, EmailDTO>
{
    private readonly EmailContext _ctx;
    private readonly IMapper _map;
    private readonly ILogger<UpdateEmailHandler> _log;

    public UpdateEmailHandler(EmailContext ctx, IMapper map, ILogger<UpdateEmailHandler> log)
    {
        _ctx = ctx;
        _map = map;
        _log = log;
    }

    public async Task<EmailDTO> Handle(UpdateEmailCommand request, CancellationToken ct)
    {
        var entity = await _ctx
            .Emails.Where(e => e.Id == request.Id && e.CompanyId == request.CompanyId)
            .FirstOrDefaultAsync(ct);

        if (entity is null)
            throw new KeyNotFoundException("Email not found or access denied");

        // Solo permitir actualizar emails en estado Pending
        if (entity.Status != EmailServices.Domain.EmailStatus.Pending)
            throw new InvalidOperationException("Cannot update email that has been sent or failed");

        // Verificar permisos: solo el creador puede actualizar
        if (entity.CreatedByTaxUserId != request.LastModifiedByTaxUserId)
        {
            throw new UnauthorizedAccessException("You can only update emails you created");
        }

        // Verificar que la nueva configuración existe y pertenece a la misma compañía
        var config = await _ctx
            .EmailConfigs.Where(c =>
                c.Id == request.EmailDto.ConfigId && c.CompanyId == request.CompanyId && c.IsActive
            )
            .FirstOrDefaultAsync(ct);

        if (config is null)
            throw new KeyNotFoundException(
                "Email configuration not found or not active for this company"
            );

        // Preservar campos importantes antes del mapeo
        var originalId = entity.Id;
        var originalStatus = entity.Status;
        var originalCreatedOn = entity.CreatedOn;
        var originalCreatedByTaxUserId = entity.CreatedByTaxUserId;
        var originalCompanyId = entity.CompanyId;
        var originalSentByTaxUserId = entity.SentByTaxUserId;

        // Mapear los cambios
        _map.Map(request.EmailDto, entity);

        // Restaurar campos que no deben cambiar y actualizar auditoría
        entity.Id = originalId;
        entity.Status = originalStatus;
        entity.CreatedOn = originalCreatedOn;
        entity.CreatedByTaxUserId = originalCreatedByTaxUserId;
        entity.CompanyId = originalCompanyId;
        entity.SentByTaxUserId = originalSentByTaxUserId;
        entity.LastModifiedByTaxUserId = request.LastModifiedByTaxUserId;
        entity.UpdatedOn = DateTime.UtcNow;

        await _ctx.SaveChangesAsync(ct);

        _log.LogInformation(
            $"Email {entity.Id} updated by user {request.LastModifiedByTaxUserId} from company {request.CompanyId}"
        );
        return _map.Map<EmailDTO>(entity);
    }
}
