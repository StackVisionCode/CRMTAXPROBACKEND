using Application.Common.DTO;
using AutoMapper;
using Infrastructure.Context;
using Infrastructure.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class GetIncomingEmailsHandler
    : IRequestHandler<GetIncomingEmailsQuery, IEnumerable<IncomingEmailDTO>>
{
    private readonly EmailContext _ctx;
    private readonly IMapper _map;
    private readonly ILogger<GetIncomingEmailsHandler> _log;

    public GetIncomingEmailsHandler(
        EmailContext ctx,
        IMapper map,
        ILogger<GetIncomingEmailsHandler> log
    )
    {
        _ctx = ctx;
        _map = map;
        _log = log;
    }

    public async Task<IEnumerable<IncomingEmailDTO>> Handle(
        GetIncomingEmailsQuery query,
        CancellationToken ct
    )
    {
        _log.LogInformation("🔍 GetIncomingEmails - CompanyId: {CompanyId}, TaxUserId: {TaxUserId}, IsRead: {IsRead}", 
        query.CompanyId, query.TaxUserId, query.IsRead);
        var dbQuery = _ctx.IncomingEmails.Where(e => e.CompanyId == query.CompanyId);

        // Filtros adicionales
        if (query.TaxUserId.HasValue)
            dbQuery = dbQuery.Where(e => e.CreatedByTaxUserId == query.TaxUserId);

        if (query.IsRead.HasValue)
            dbQuery = dbQuery.Where(e => e.IsRead == query.IsRead);

        var emails = await dbQuery.OrderByDescending(e => e.ReceivedOn).ToListAsync(ct);
        _log.LogInformation("📧 Found {Count} incoming emails in database", emails.Count);

        // Obtener attachments para todos los emails en una sola query
        var emailIds = emails.Select(e => e.Id).ToList();
        var attachments = await _ctx
            .EmailAttachments.Where(a =>
                emailIds.Contains(a.EmailId) && a.CompanyId == query.CompanyId
            )
            .ToListAsync(ct);

        // Mapear emails y asignar attachments
        var emailDtos = _map.Map<List<IncomingEmailDTO>>(emails);
        var attachmentDtos = _map.Map<List<EmailAttachmentDTO>>(attachments);

        // Agrupar attachments por EmailId
        var attachmentsByEmail = attachmentDtos
            .GroupBy(a => a.EmailId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Asignar attachments a cada email
        foreach (var emailDto in emailDtos)
        {
            emailDto.Attachments = attachmentsByEmail.GetValueOrDefault(
                emailDto.Id,
                new List<EmailAttachmentDTO>()
            );
        }

        _log.LogInformation(
            $"Retrieved {emails.Count} incoming emails for company {query.CompanyId}"
        );

        _log.LogInformation("📧 Returning {Count} DTOs to frontend", emailDtos.Count);

        return emailDtos;
    }
}
