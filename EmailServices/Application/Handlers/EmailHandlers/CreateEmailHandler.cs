using Application.Common.DTO;
using AutoMapper;
using Domain;
using Infrastructure.Commands;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class CreateEmailHandler : IRequestHandler<CreateEmailCommand, EmailDTO>
{
    private readonly EmailContext _ctx;
    private readonly IMapper _map;
    private readonly ILogger<CreateEmailHandler> _log;

    public CreateEmailHandler(EmailContext ctx, IMapper map, ILogger<CreateEmailHandler> log)
    {
        _ctx = ctx;
        _map = map;
        _log = log;
    }

    public async Task<EmailDTO> Handle(CreateEmailCommand request, CancellationToken ct)
    {
        // Verificar configuración existente con CompanyId y que sea activa
        var cfg = await _ctx
            .EmailConfigs.Where(c =>
                c.Id == request.EmailDto.ConfigId && c.CompanyId == request.CompanyId && c.IsActive
            )
            .FirstOrDefaultAsync(ct);

        if (cfg is null)
            throw new KeyNotFoundException(
                "Email configuration not found or not active for this company"
            );

        var entity = _map.Map<Email>(request.EmailDto);

        // Establecer auditoría y campos obligatorios
        entity.CompanyId = request.CompanyId;
        entity.CreatedByTaxUserId = request.CreatedByTaxUserId;
        entity.SentByTaxUserId = request.CreatedByTaxUserId;
        entity.Status = EmailServices.Domain.EmailStatus.Pending;
        entity.CreatedOn = DateTime.UtcNow;

        _ctx.Emails.Add(entity);
        await _ctx.SaveChangesAsync(ct);

        _log.LogInformation(
            $"Email {entity.Id} created by user {request.CreatedByTaxUserId} for company {request.CompanyId}"
        );

        var dto = _map.Map<EmailDTO>(entity);
        return dto;
    }
}
