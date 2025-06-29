using Infrastructure.Commands;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class DeleteEmailConfigCommandHandler : IRequestHandler<DeleteEmailConfigCommand, Unit>
{
    private readonly EmailContext _ctx;
    private readonly ILogger<DeleteEmailConfigCommandHandler> _log;

    public DeleteEmailConfigCommandHandler(
        EmailContext ctx,
        ILogger<DeleteEmailConfigCommandHandler> log
    )
    {
        _ctx = ctx;
        _log = log;
    }

    public async Task<Unit> Handle(DeleteEmailConfigCommand r, CancellationToken ct)
    {
        var entity = await _ctx.EmailConfigs.FirstOrDefaultAsync(c => c.Id == r.Id, ct);
        if (entity is null)
            throw new KeyNotFoundException("Config not found");

        _ctx.EmailConfigs.Remove(entity);
        await _ctx.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
