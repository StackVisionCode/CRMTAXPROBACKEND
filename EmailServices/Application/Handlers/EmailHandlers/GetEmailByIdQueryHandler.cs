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
    private readonly ILogger<GetEmailByIdQueryHandler> _log;

    public GetEmailByIdQueryHandler(
        EmailContext ctx,
        IMapper map,
        ILogger<GetEmailByIdQueryHandler> log
    )
    {
        _ctx = ctx;
        _map = map;
        _log = log;
    }

    public async Task<EmailDTO?> Handle(GetEmailByIdQuery q, CancellationToken ct)
    {
        var entity = await _ctx
            .Emails.Where(e => e.Id == q.EmailId && e.CompanyId == q.CompanyId)
            .FirstOrDefaultAsync(ct);

        return entity is null ? null : _map.Map<EmailDTO>(entity);
    }
}
