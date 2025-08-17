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
        var entity = await _ctx
            .EmailConfigs.Where(c => c.Id == r.Id && c.CompanyId == r.CompanyId && c.IsActive)
            .FirstOrDefaultAsync(ct);

        if (entity is null)
            throw new KeyNotFoundException("Email configuration not found or access denied");

        // Verificar si hay emails usando esta configuración
        var emailsUsingConfig = await _ctx
            .Emails.Where(e => e.ConfigId == r.Id && e.CompanyId == r.CompanyId)
            .AnyAsync(ct);

        if (emailsUsingConfig)
        {
            // Soft delete en lugar de hard delete si hay emails asociados
            entity.IsActive = false;
            entity.LastModifiedByTaxUserId = r.DeletedByTaxUserId;
            entity.UpdatedOn = DateTime.UtcNow;

            _log.LogInformation(
                $"Email configuration {r.Id} deactivated by user {r.DeletedByTaxUserId} from company {r.CompanyId}"
            );
        }
        else
        {
            // Hard delete si no hay emails asociados
            _ctx.EmailConfigs.Remove(entity);

            _log.LogInformation(
                $"Email configuration {r.Id} permanently deleted by user {r.DeletedByTaxUserId} from company {r.CompanyId}"
            );
        }

        await _ctx.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
