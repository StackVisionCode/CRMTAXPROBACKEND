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
        var email = await _ctx
            .IncomingEmails.Where(e => e.Id == request.Id && e.CompanyId == request.CompanyId)
            .FirstOrDefaultAsync(ct);

        if (email is null)
            throw new KeyNotFoundException("Incoming email not found or access denied");

        email.IsRead = true;
        await _ctx.SaveChangesAsync(ct);

        _log.LogInformation(
            $"Incoming email {request.Id} marked as read by user {request.ModifiedByTaxUserId} from company {request.CompanyId}"
        );
        return Unit.Value;
    }
}
