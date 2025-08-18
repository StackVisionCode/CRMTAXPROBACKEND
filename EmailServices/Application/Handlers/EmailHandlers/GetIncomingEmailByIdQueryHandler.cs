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
    private readonly ILogger<GetIncomingEmailByIdQueryHandler> _log;

    public GetIncomingEmailByIdQueryHandler(
        EmailContext ctx,
        IMapper map,
        ILogger<GetIncomingEmailByIdQueryHandler> log
    )
    {
        _ctx = ctx;
        _map = map;
        _log = log;
    }

    public async Task<IncomingEmailDTO?> Handle(
        GetIncomingEmailByIdQuery query,
        CancellationToken ct
    )
    {
        // Query eficiente sin Include - primero obtenemos el email
        var email = await _ctx
            .IncomingEmails.Where(e => e.Id == query.Id && e.CompanyId == query.CompanyId)
            .FirstOrDefaultAsync(ct);

        if (email is null)
            return null;

        // Luego obtenemos los attachments en una query separada
        var attachments = await _ctx
            .EmailAttachments.Where(a => a.EmailId == query.Id && a.CompanyId == query.CompanyId)
            .ToListAsync(ct);

        var dto = _map.Map<IncomingEmailDTO>(email);
        dto.Attachments = _map.Map<List<EmailAttachmentDTO>>(attachments);

        return dto;
    }
}
