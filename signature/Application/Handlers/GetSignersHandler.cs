using Application.DTOs.ReadDTOs;
using Application.Helpers;
using Infrastructure.Context;
using Infrastruture.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetSignersHandler
    : IRequestHandler<GetSignersQuery, ApiResponse<List<SignerListItemDto>>>
{
    private readonly ILogger<GetSignersHandler> _logger;
    private readonly SignatureDbContext _db;

    public GetSignersHandler(ILogger<GetSignersHandler> logger, SignatureDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task<ApiResponse<List<SignerListItemDto>>> Handle(
        GetSignersQuery request,
        CancellationToken ct
    )
    {
        // Consulta optimizada usando projection directo
        var list = await (
            from s in _db.Signers
            join r in _db.SignatureRequests on s.SignatureRequestId equals r.Id
            orderby s.Order
            select new SignerListItemDto
            {
                Id = s.Id,
                SignatureRequestId = s.SignatureRequestId,
                DocumentId = r.DocumentId,
                Email = s.Email,
                FullName = s.FullName,
                Order = s.Order,
                Status = s.Status,
                CreatedAt = s.CreatedAt,
                SignedAtUtc = s.SignedAtUtc,
                RejectedAtUtc = s.RejectedAtUtc,
                RejectedReason = s.RejectReason,
                RequestStatus = r.Status,
                RequestCreatedAt = r.CreatedAt,
                RequestUpdatedAt = r.UpdatedAt,
                BoxesCount = s.Boxes.Count(),
                SignatureBoxesCount = s.Boxes.Count(b => b.Kind == BoxKind.Signature),
                InitialsBoxesCount = s.Boxes.Count(b => b.Kind == BoxKind.Initials),
                DateBoxesCount = s.Boxes.Count(b => b.Kind == BoxKind.Date),
            }
        ).AsNoTracking().ToListAsync(ct);

        _logger.LogInformation("Fetched {Count} signers", list.Count);

        return ApiResponse<List<SignerListItemDto>>.Ok(list);
    }
}
