using Infrastructure.Commands;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class DeleteEmailHandler : IRequestHandler<DeleteEmailCommand, Unit>
{
    private readonly EmailContext _ctx;
    private readonly ILogger<DeleteEmailHandler> _log;

    public DeleteEmailHandler(EmailContext ctx, ILogger<DeleteEmailHandler> log)
    {
        _ctx = ctx;
        _log = log;
    }

    public async Task<Unit> Handle(DeleteEmailCommand request, CancellationToken ct)
    {
        var entity = await _ctx.Emails.FirstOrDefaultAsync(e => e.Id == request.Id, ct);
        if (entity is null)
            throw new KeyNotFoundException("Email not found");

        // Solo permitir eliminar emails en estado Pending o Failed
        if (entity.Status == EmailServices.Domain.EmailStatus.Sent)
            throw new InvalidOperationException("Cannot delete sent emails");

        _ctx.Emails.Remove(entity);
        await _ctx.SaveChangesAsync(ct);

        _log.LogInformation($"Email {request.Id} deleted successfully");
        return Unit.Value;
    }
}
