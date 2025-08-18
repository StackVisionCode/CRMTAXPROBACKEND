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
        try
        {
            var result = await (
                from sr in _context.SignatureRequests
                join s in _context.Signers on sr.Id equals s.SignatureRequestId into signerGroup
                select new SignatureRequestSummaryDto
                {
                    Id = sr.Id,
                    DocumentId = sr.DocumentId,
                    Status = sr.Status,
                    CreatedAt = sr.CreatedAt,
                    UpdatedAt = sr.UpdatedAt,
                    RejectedAtUtc = sr.RejectedAtUtc,
                    RejectReason = sr.RejectReason,
                    RejectedBySignerId = sr.RejectedBySignerId,
                    CompanyId = sr.CompanyId,
                    CreatedByTaxUserId = sr.CreatedByTaxUserId,
                    LastModifiedByTaxUserId = sr.LastModifiedByTaxUserId,
                    SignerCount = signerGroup.Count(),
                    SignedCount = signerGroup.Count(x => x.Status == SignerStatus.Signed),
                }
            ).OrderByDescending(x => x.CreatedAt).AsNoTracking().ToListAsync(cancellationToken);

            if (result is null || !result.Any())
            {
                _logger.LogInformation("No signature requests found");
                return new ApiResponse<List<SignatureRequestSummaryDto>>(
                    false,
                    "No signature requests found",
                    new List<SignatureRequestSummaryDto>()
                );
            }

            _logger.LogInformation("Retrieved {Count} signature requests", result.Count);

            return new ApiResponse<List<SignatureRequestSummaryDto>>(
                true,
                "Signature requests retrieved successfully",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving signature requests: {Message}", ex.Message);
            return new ApiResponse<List<SignatureRequestSummaryDto>>(
                false,
                ex.Message,
                new List<SignatureRequestSummaryDto>()
            );
        }
    }
}
