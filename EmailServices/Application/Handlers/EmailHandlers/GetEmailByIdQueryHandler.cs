using Application.Common.DTO;
using AutoMapper;
using Infrastructure.Context;
using Infrastructure.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class GetEmailByIdQueryHandler : IRequestHandler<GetEmailByIdQuery, EmailDTO?>
{
    private readonly EmailContext _ctx;
    private readonly IMapper _map;

    public GetEmailByIdQueryHandler(EmailContext ctx, IMapper map)
    {
        _ctx = ctx;
        _map = map;
    }

    public async Task<EmailDTO?> Handle(GetEmailByIdQuery q, CancellationToken ct)
    {
        var entity = await _ctx.Emails
                            .FirstOrDefaultAsync(e => e.Id == q.EmailId, ct);
        return entity is null ? null : _map.Map<EmailDTO>(entity);
    }
}