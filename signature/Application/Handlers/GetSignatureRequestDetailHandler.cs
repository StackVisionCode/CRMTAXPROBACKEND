using Application.DTOs.ReadDTOs;
using Application.Helpers;
using Infrastructure.Context;
using Infrastruture.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class GetSignatureRequestDetailHandler
    : IRequestHandler<GetSignatureRequestDetailQuery, ApiResponse<SignatureRequestDetailDto>>
{
    private readonly ILogger<GetSignatureRequestsHandler> _logger;
    private readonly SignatureDbContext _context;

    public GetSignatureRequestDetailHandler(
        ILogger<GetSignatureRequestsHandler> logger,
        SignatureDbContext context
    )
    {
        _logger = logger;
        _context = context;
    }

    public async Task<ApiResponse<SignatureRequestDetailDto>> Handle(
        GetSignatureRequestDetailQuery request,
        CancellationToken cancellationToken
    )
    {
        var header = await _context
            .SignatureRequests.AsNoTracking()
            .Where(r => r.Id == request.RequestId)
            .Select(r => new
            {
                r.Id,
                r.DocumentId,
                r.Status,
                r.CreatedAt,
                r.UpdatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (header is null)
        {
            _logger.LogWarning(
                "Signature request with RequestId: {RequestId} not found",
                request.RequestId
            );
            return new(false, "Solicitud no encontrada");
        }

        var signers = await _context
            .Signers.AsNoTracking()
            .Where(s => s.SignatureRequestId == header.Id)
            .Select(s => new SignerSummaryDto
            {
                Id = s.Id,
                Email = s.Email,
                Order = s.Order,
                Status = s.Status,
                CreatedAt = s.CreatedAt,
                SignedAtUtc = s.SignedAtUtc,
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Signature request with RequestId: {RequestId} found with {SignerCount} signers",
            request.RequestId,
            signers.Count
        );

        var dto = new SignatureRequestDetailDto
        {
            Id = header.Id,
            DocumentId = header.DocumentId,
            Status = header.Status,
            CreatedAt = header.CreatedAt,
            UpdatedAt = header.UpdatedAt,
            Signers = signers,
        };

        _logger.LogInformation(
            "Signature request detail for RequestId: {RequestId} retrieved successfully",
            request.RequestId
        );
        return new ApiResponse<SignatureRequestDetailDto>(true, "Solicitud encontrada", dto);
    }
}
