using Application.DTOs.ReadDTOs;
using Application.Helpers;
using Infrastructure.Context;
using Infrastruture.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

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
        var data = await (
            from r in _context.SignatureRequests
            join s in _context.Signers on r.Id equals s.SignatureRequestId into g
            select new SignatureRequestSummaryDto
            {
                Id = r.Id,
                DocumentId = r.DocumentId,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                SignerCount = g.Count(),
                SignedCount = g.Count(x => x.Status == SignerStatus.Signed),
                RejectedAtUtc = r.RejectedAtUtc,
                RejectReason = r.RejectReason,
                RejectedBySignerId = r.RejectedBySignerId,
            }
        ).AsNoTracking().ToListAsync(cancellationToken);

        if (data == null || !data.Any())
        {
            _logger.LogWarning("No signature requests found.");
            return ApiResponse<List<SignatureRequestSummaryDto>>.Fail(
                "No signature requests found."
            );
        }

        _logger.LogInformation("Retrieved {Count} signature requests.", data.Count);
        return ApiResponse<List<SignatureRequestSummaryDto>>.Ok(data);
    }
}
