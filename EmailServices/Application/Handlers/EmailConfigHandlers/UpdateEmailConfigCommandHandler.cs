using Application.Common.DTO;
using Application.Validation;
using AutoMapper;
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
        var entity =
            await _ctx.EmailConfigs.FirstOrDefaultAsync(c => c.Id == r.Id, ct)
            ?? throw new KeyNotFoundException("Config not found");

        _validator.Validate(r.Config);

        _map.Map(r.Config, entity);
        await _ctx.SaveChangesAsync(ct);
        return _map.Map<EmailConfigDTO>(entity);
    }
}
