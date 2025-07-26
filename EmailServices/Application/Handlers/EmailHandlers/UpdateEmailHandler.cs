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
        var entity = await _ctx.Emails.FirstOrDefaultAsync(e => e.Id == request.Id, ct);
        if (entity is null)
            throw new KeyNotFoundException("Email not found");

        // Solo permitir actualizar emails en estado Pending
        if (entity.Status != EmailServices.Domain.EmailStatus.Pending)
            throw new InvalidOperationException("Cannot update email that has been sent or failed");

        // Verificar que la configuración existe
        var config = await _ctx.EmailConfigs.FindAsync(
            new object[] { request.EmailDto.ConfigId },
            ct
        );
        if (config is null)
            throw new KeyNotFoundException("Email configuration not found");

        // Mapear los cambios, pero preservar ciertos campos
        var originalStatus = entity.Status;
        var originalCreatedOn = entity.CreatedOn;
        var originalSentByUserId = entity.SentByUserId;

        _map.Map(request.EmailDto, entity);

        // Restaurar campos que no deben cambiar
        entity.Status = originalStatus;
        entity.CreatedOn = originalCreatedOn;
        entity.SentByUserId = originalSentByUserId;

        await _ctx.SaveChangesAsync(ct);

        _log.LogInformation($"Email {entity.Id} updated successfully");
        return _map.Map<EmailDTO>(entity);
    }
}
