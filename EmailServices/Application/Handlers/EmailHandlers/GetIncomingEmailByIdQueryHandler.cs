using Application.Common.DTO;
using AutoMapper;
using Infrastructure.Context;
using Infrastructure.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class GetIncomingEmailByIdQueryHandler
    : IRequestHandler<GetIncomingEmailByIdQuery, IncomingEmailDTO?>
{
    private readonly EmailContext _ctx;
    private readonly IMapper _map;

    public GetIncomingEmailByIdQueryHandler(EmailContext ctx, IMapper map)
    {
        _ctx = ctx;
        _map = map;
    }

    public async Task<IncomingEmailDTO?> Handle(
        GetIncomingEmailByIdQuery query,
        CancellationToken ct
    )
    {
        var email = await _ctx
            .IncomingEmails.Include(e => e.Attachments)
            .FirstOrDefaultAsync(e => e.Id == query.Id, ct);

        return email is null ? null : _map.Map<IncomingEmailDTO>(email);
    }
}
