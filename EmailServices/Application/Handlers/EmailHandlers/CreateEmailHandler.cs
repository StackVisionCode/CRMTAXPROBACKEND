using Application.Common.DTO;
using AutoMapper;
using Domain;
using Infrastructure.Commands;
using Infrastructure.Context;
using MediatR;
using Microsoft.Extensions.Logging;

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
        // verifica configuración existente
        var cfg = await _ctx.EmailConfigs.FindAsync(new object[] { request.EmailDto.ConfigId }, ct);

        if (cfg is null)
            throw new KeyNotFoundException("Email configuration not found");

        var entity = _map.Map<Email>(request.EmailDto);
        entity.SentByUserId = request.EmailDto.UserId;
        entity.Status = EmailServices.Domain.EmailStatus.Pending;
        entity.CreatedOn = DateTime.UtcNow;

        _ctx.Emails.Add(entity);
        await _ctx.SaveChangesAsync(ct);

        var dto = _map.Map<EmailDTO>(entity);
        return dto;
    }
}
