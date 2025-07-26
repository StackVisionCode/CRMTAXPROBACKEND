using Infrastructure.Commands;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class MarkIncomingEmailAsReadCommandHandler
    : IRequestHandler<MarkIncomingEmailAsReadCommand, Unit>
{
    private readonly EmailContext _ctx;
    private readonly ILogger<MarkIncomingEmailAsReadCommandHandler> _log;

    public MarkIncomingEmailAsReadCommandHandler(
        EmailContext ctx,
        ILogger<MarkIncomingEmailAsReadCommandHandler> log
    )
    {
        _ctx = ctx;
        _log = log;
    }

    public async Task<Unit> Handle(MarkIncomingEmailAsReadCommand request, CancellationToken ct)
    {
        var email = await _ctx.IncomingEmails.FirstOrDefaultAsync(e => e.Id == request.Id, ct);
        if (email is null)
            throw new KeyNotFoundException("Incoming email not found");

        email.IsRead = true;
        await _ctx.SaveChangesAsync(ct);

        _log.LogInformation($"Incoming email {request.Id} marked as read");
        return Unit.Value;
    }
}
