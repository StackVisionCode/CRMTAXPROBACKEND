using Application.DTOs.ReadDTOs;
using Application.Helpers;
using Infrastructure.Context;
using Infrastruture.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetSignatureRequestsHandler
    : IRequestHandler<GetSignatureRequestsQuery, ApiResponse<List<SignatureRequestSummaryDto>>>
{
    private readonly ILogger<GetSignatureRequestsHandler> _logger;
    private readonly SignatureDbContext _context;

    public GetSignatureRequestsHandler(
        ILogger<GetSignatureRequestsHandler> logger,
        SignatureDbContext context
    )
    {
        _logger = logger;
        _context = context;
    }

    public async Task<ApiResponse<List<SignatureRequestSummaryDto>>> Handle(
        GetSignatureRequestsQuery request,
        CancellationToken cancellationToken
    )
    {
        var query =
            from r in _context.SignatureRequests
            join s in _context.Signers on r.Id equals s.SignatureRequestId into g
            select new
            {
                r.Id,
                r.DocumentId,
                r.Status,
                r.CreatedAt,
                r.UpdatedAt,
                r.RejectedAtUtc,
                r.RejectReason,
                r.RejectedBySignerId,
                SignerCount = g.Count(),
                SignedCount = g.Count(x => x.Status == SignerStatus.Signed),
            };

        var data = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new SignatureRequestSummaryDto
            {
                Id = x.Id,
                DocumentId = x.DocumentId,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                SignerCount = x.SignerCount,
                SignedCount = x.SignedCount,
                RejectedAtUtc = x.RejectedAtUtc,
                RejectReason = x.RejectReason,
                RejectedBySignerId = x.RejectedBySignerId,
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} signature requests.", data.Count);

        return ApiResponse<List<SignatureRequestSummaryDto>>.Ok(data);
    }
}
