using Application.Common.DTO;
using Application.Validation;
using AutoMapper;
using Domain;
using Infrastructure.Commands;
using Infrastructure.Context;
using MediatR;

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
        _validator.Validate(r.Config);

        var entity = _map.Map<EmailConfig>(r.Config);

        _ctx.EmailConfigs.Add(entity);
        await _ctx.SaveChangesAsync(ct);
        return _map.Map<EmailConfigDTO>(entity);
    }
}
